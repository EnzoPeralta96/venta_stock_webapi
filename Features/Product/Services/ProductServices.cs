using AutoMapper;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Product.DTO;
using proyecto_venta_stock.Product.ProductRepository;
using proyecto_venta_stock.Category.CategoryRepository;
using proyecto_venta_stock.Location.LocationRepository;
using venta_stock_webapi.Shared.Extensions;
using venta_stock_webapi.Shared.Paged;
using OfficeOpenXml;
using System.Text;
using System.Text.Json;
using System.ComponentModel.DataAnnotations;
using proyecto_venta_stock.Configuration;
using Microsoft.Extensions.Options;
using proyecto_venta_stock.Data;
using venta_stock_webapi.Features.StockMovement.Services;
using Microsoft.EntityFrameworkCore;
using venta_stock_webapi.Features.Audit.Repository;
using venta_stock_webapi.Shared.Identity;

namespace proyecto_venta_stock.Product.Services
{
    public class ProductServices : IProductServices
    {
        private readonly ILogger<ProductServices> _logger;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;
        private readonly IOptions<ImportDefaultsOptions> _defaultsOptions;
        private readonly VentaStockContext _dbContext;
        private readonly IStockMovementService _stockMovementService;
        private readonly IAuditRepository _auditRepository;
        private readonly IUserContext _userContext;

        public ProductServices(IProductRepository productRepository, ILogger<ProductServices> logger, IMapper mapper, ICategoryRepository categoryRepository, ILocationRepository locationRepository, IOptions<ImportDefaultsOptions> defaultsOptions, VentaStockContext dbContext, IStockMovementService stockMovementService, IAuditRepository auditRepository, IUserContext userContext)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _locationRepository = locationRepository;
            _logger = logger;
            _mapper = mapper;
            _defaultsOptions = defaultsOptions;
            _dbContext = dbContext;
            _stockMovementService = stockMovementService;
            _auditRepository = auditRepository;
            _userContext = userContext;
        }

        private async Task LogAsync(string accion, string entidadTipo, string detalle,
            object? anterior = null, object? nuevo = null)
        {
            try
            {
                await _auditRepository.LogAsync(new Auditoria
                {
                    FechaHora         = DateTimeOffset.UtcNow,
                    IdUsuario         = _userContext.UserId,
                    UsuarioNombre     = _userContext.UserName,
                    Accion            = accion,
                    EntidadTipo       = entidadTipo,
                    Detalle           = detalle,
                    ValoresAnteriores = anterior != null ? JsonSerializer.Serialize(anterior) : null,
                    ValoresNuevos     = nuevo    != null ? JsonSerializer.Serialize(nuevo)    : null,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo registrar auditoría.");
            }
        }
        private async Task<(bool flowControl, Result<bool> value)> ValidateCreate(ProductDTO dto)
        {
            if (await _productRepository.Exists(dto.Nombre, dto.Marca))
                return (false, Result<bool>.Failure(ProductErrorCode.product_name_in_use));

            foreach (var cb in dto.CodigoBarras)
                if (await _productRepository.CodigoBarraExists(cb))
                    return (false, Result<bool>.Failure(ProductErrorCode.barcode_in_use));

            if (dto.IdCategoria == null || !await _categoryRepository.ExistsById(dto.IdCategoria.Value))
                return (false, Result<bool>.Failure(ProductErrorCode.categoria_invalida));

            var ubic = dto.IdUbicacion == null
                ? null
                : await _locationRepository.GetByIdAsync(dto.IdUbicacion.Value);

            if (ubic == null)
                return (false, Result<bool>.Failure(ProductErrorCode.ubicacion_invalida));

            return (true, null);
        }

        private async Task LogCreateAsync(Producto product, decimal stockInicial)
        {
            await LogAsync("CREACION", "PRODUCTO",
                $"Producto creado: '{product.Nombre} {product.Marca}' | Precio: ${product.Precio:N2} | Stock inicial: {stockInicial}",
                null,
                new { Nombre = product.Nombre, Marca = product.Marca, Precio = product.Precio, StockInicial = stockInicial });
        }

        public async Task<Result<bool>> Create(ProductDTO productDTO)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                (bool flowControl, Result<bool> value) = await ValidateCreate(productDTO);

                if (!flowControl) return value;

                var product = _mapper.Map<Producto>(productDTO);

                product.Stock = 0;

                await _productRepository.Create(product);

                var movResult = await _stockMovementService.RegistrarMovimientoAsync(
                    product.IdProducto,
                    TipoMovimientoStockEnum.AltaProducto,
                    productDTO.Stock,
                    "STOCK INICIAL AL DAR DE ALTA EL PRODUCTO",
                    _userContext.UserId);

                if (!movResult.IsSuccess)
                {
                    await transaction.RollbackAsync();
                    return Result<bool>.Failure(ProductErrorCode.error_inesperado);
                }

                await transaction.CommitAsync();

                await LogCreateAsync(product, productDTO.Stock);

                return Result<bool>.Success();
            }
            catch (System.Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure(ProductErrorCode.error_inesperado);
            }
        }

        private async Task<(bool flowControl, Result<bool> value)> ValidateUpdate(ProductDTO dto, Producto existing)
        {
            bool productExists = await _productRepository.Exists(dto.Nombre, dto.Marca);

            if (productExists && (existing.Nombre != dto.Nombre || existing.Marca != dto.Marca))
                return (false, Result<bool>.Failure(ProductErrorCode.product_name_in_use));

            var codigosNuevos = dto.CodigoBarras
                .Where(cb => !existing.CodigoBarras.Any(e => e.Codigo == cb.Codigo))
                .ToList();

            foreach (var cb in codigosNuevos)
                if (await _productRepository.CodigoBarraExists(cb))
                    return (false, Result<bool>.Failure(ProductErrorCode.barcode_in_use));

            if (dto.IdCategoria == null || !await _categoryRepository.ExistsById(dto.IdCategoria.Value))
                return (false, Result<bool>.Failure(ProductErrorCode.categoria_invalida));

            var ubic = dto.IdUbicacion == null
                ? null
                : await _locationRepository.GetByIdAsync(dto.IdUbicacion.Value);
            if (ubic == null)
                return (false, Result<bool>.Failure(ProductErrorCode.ubicacion_invalida));

            return (true, null);
        }

        private (Dictionary<string, object?> anterior, Dictionary<string, object?> nuevo) ApplyAndTrackChangesProduct(Producto existing, ProductDTO dto)
        {
            var anterior = new Dictionary<string, object?>();
            var nuevo    = new Dictionary<string, object?>();

            void Track(string campo, object? old, object? newVal)
            {
                if (!Equals(old, newVal)) { anterior[campo] = old; nuevo[campo] = newVal; }
            }

            Track("Nombre",        existing.Nombre,        dto.Nombre);
            Track("Marca",         existing.Marca,          dto.Marca);
            Track("Precio",        existing.Precio,         dto.Precio);
            Track("IdCategoria",   existing.IdCategoria,    dto.IdCategoria);
            Track("IdUbicacion",   existing.IdUbicacion,    dto.IdUbicacion);
            Track("StockMinimo",   existing.StockMinimo,    dto.StockMinimo);
            Track("VentaSinStock", existing.VentaSinStock,  dto.VentaSinStock);

            existing.Nombre             = dto.Nombre;
            existing.Marca              = dto.Marca;
            existing.Descripcion        = dto.Descripcion;
            existing.Precio             = dto.Precio;
            existing.StockMinimo        = dto.StockMinimo;
            existing.IdCategoria        = dto.IdCategoria;
            existing.IdUbicacion        = dto.IdUbicacion;
            existing.VentaSinStock      = dto.VentaSinStock;
            existing.IdUnidadMedida     = dto.IdUnidadMedida;
            existing.Costo              = dto.Costo;
            existing.PorcentajeGanancia = dto.PorcentajeGanancia;

            var codigosAgregar = dto.CodigoBarras
                .Where(d => !existing.CodigoBarras.Any(e => e.Codigo == d.Codigo))
                .ToList();
            var codigosEliminar = existing.CodigoBarras
                .Where(e => !dto.CodigoBarras.Any(d => d.Codigo == e.Codigo))
                .ToList();

            foreach (var c in codigosEliminar)
                existing.CodigoBarras.Remove(c);
            foreach (var c in codigosAgregar)
                existing.CodigoBarras.Add(new CodigoBarra { Codigo = c.Codigo, IdProducto = existing.IdProducto });

            return (anterior, nuevo);
        }

        private async Task LogUpdateAsync(ProductDTO dto, Dictionary<string, object?> anterior, Dictionary<string, object?> nuevo)
        {
            string accion = anterior.ContainsKey("Precio") ? "PRECIO_ACTUALIZADO"
                : anterior.ContainsKey("Nombre") ? "NOMBRE_ACTUALIZADO"
                : "ACTUALIZACION";

            string detalle = $"Producto actualizado: '{dto.Nombre} {dto.Marca}'";
            if (anterior.Count > 0)
                detalle += " | " + string.Join(" | ", anterior.Keys.Select(k =>
                    k == "Precio"
                        ? $"Precio: ${anterior[k]:N2} → ${nuevo[k]:N2}"
                        : $"{k}: '{anterior[k]}' → '{nuevo[k]}'"));

            await LogAsync(accion, "PRODUCTO", detalle,
                anterior.Count > 0 ? anterior : null,
                nuevo.Count    > 0 ? nuevo    : null);
        }

        public async Task<Result<bool>> Update(ProductDTO productDTO)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existingProduct = await _productRepository.GetById(productDTO.IdProducto);

                if (existingProduct == null) return Result<bool>.Failure(ProductErrorCode.product_not_found);

                (bool flowControl, Result<bool> value) = await ValidateUpdate(productDTO, existingProduct);

                if (!flowControl) return value;

                var (anterior, nuevo) = ApplyAndTrackChangesProduct(existingProduct, productDTO);

                await _productRepository.Update(existingProduct);

                await transaction.CommitAsync();

                await LogUpdateAsync(productDTO, anterior, nuevo);

                return Result<bool>.Success();
            }
            catch (System.Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure(ProductErrorCode.error_inesperado);
            }
        }

        public async Task<Result<List<ProductDTO>>> GetAll()
        {
            try
            {
                var products = await _productRepository.GetAll();
                var dtos = _mapper.Map<List<ProductDTO>>(products);
                return Result<List<ProductDTO>>.Success(dtos);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<List<ProductDTO>>.Failure(ProductErrorCode.error_inesperado);
            }
        }

        public async Task<Result<List<ProductDetailDTO>>> GetAllWithCategoryAndLocation(bool? activo = true)
        {
            try
            {
                var products = await _productRepository.GetAllWithCategoryAndLocation(activo);
                var dtos = _mapper.Map<List<ProductDetailDTO>>(products);
                return Result<List<ProductDetailDTO>>.Success(dtos);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<List<ProductDetailDTO>>.Failure(ProductErrorCode.error_inesperado);
            }
        }

        public async Task<Result<ProductDTO>> GetById(int idProducto)
        {
            try
            {
                var product = await _productRepository.GetById(idProducto);
                if (product == null) return Result<ProductDTO>.Failure(ProductErrorCode.product_not_found);
                var dto = _mapper.Map<ProductDTO>(product);
                return Result<ProductDTO>.Success(dto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<ProductDTO>.Failure(ProductErrorCode.error_inesperado);
            }
        }

        public async Task<Result<bool>> Delete(int idProducto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await _productRepository.GetById(idProducto);
                if (existing == null)
                    return Result<bool>.Failure(ProductErrorCode.product_not_found);

                await _productRepository.Delete(existing);
                await transaction.CommitAsync();

                await LogAsync("BAJA", "PRODUCTO",
                    $"Producto dado de baja: '{existing.Nombre} {existing.Marca}'",
                    new { Nombre = existing.Nombre, Marca = existing.Marca, Activo = true },
                    new { Activo = false });

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex);
                return Result<bool>.Failure(ProductErrorCode.error_inesperado);
            }
        }
        private async Task LogToggleEstadoAsync(Producto product, bool eraActivo)
        {
            if (eraActivo)
                await LogAsync("BAJA", "PRODUCTO",
                    $"Producto desactivado: '{product.Nombre} {product.Marca}'",
                    new { Activo = true }, new { Activo = false });
            else
                await LogAsync("REACTIVACION", "PRODUCTO",
                    $"Producto reactivado: '{product.Nombre} {product.Marca}'",
                    new { Activo = false }, new { Activo = true });
        }

        public async Task<Result<bool>> ToggleEstado(int idProducto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var existing = await _productRepository.GetById(idProducto);
                
                if (existing == null)
                    return Result<bool>.Failure(ProductErrorCode.product_not_found);

                bool eraActivo = existing.Activo;
                existing.Activo = !existing.Activo;
                await _productRepository.Update(existing);
                await transaction.CommitAsync();

                await LogToggleEstadoAsync(existing, eraActivo);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex);
                return Result<bool>.Failure(ProductErrorCode.error_inesperado);
            }
        }

        public async Task<Result<PagedList<ProductDetailDTO>>> GetAllWithCategoryAndLocationPaged(
            int pageIndex,
            int pageSize,
            bool? activo = true,
            string? search = null,
            int? idCategoria = null)
        {
            try
            {
                var query = _productRepository.QueryAllWithCategoryAndLocation(activo);

                if (idCategoria.HasValue)
                    query = query.Where(p => p.IdCategoria == idCategoria.Value);

                // aplicar búsqueda si hay
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lower = search.ToLower();
                    query = query.Where(p =>
                        p.Nombre.ToLower().Contains(lower) ||
                        p.Marca.ToLower().Contains(lower) ||
                        (p.Descripcion != null && p.Descripcion.ToLower().Contains(lower)) ||
                        p.CodigoBarras.Any(cb => cb.Codigo.ToLower().Contains(lower))
                    );
                }

                var paged = await PagedList<Producto>.CreateAsync(query, pageIndex, pageSize);
                var dtoItems = _mapper.Map<List<ProductDetailDTO>>(paged.Items);

                var dtoPaged = new PagedList<ProductDetailDTO>(
                    dtoItems, paged.TotalCount, paged.PagedIndex, paged.PageSize);

                return Result<PagedList<ProductDetailDTO>>.Success(dtoPaged);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex);
                return Result<PagedList<ProductDetailDTO>>.Failure(ProductErrorCode.error_inesperado);
            }
        }

        public async Task<byte[]> ExportarCsvAsync()
        {
            var productos = await _productRepository.GetAllWithCodigosAsync();

            var sb = new StringBuilder();

            sb.AppendLine("CodigoBarra;Nombre;Marca;Costo;MargenGanancia;Precio;Categoria;Ubicacion;Stock;UnidadMedida");

            foreach (var p in productos)
            {
                string codigo     = p.CodigoBarras.FirstOrDefault()?.Codigo ?? "";
                string categoria  = p.IdCategoriaNavigation?.Categoria ?? "";
                string ubicacion  = p.IdUbicacionNavigation != null
                    ? $"Fila {p.IdUbicacionNavigation.Fila} - Sección {p.IdUbicacionNavigation.Seccion} - Nivel {p.IdUbicacionNavigation.Nivel}"
                    : "";
                string unidad     = p.IdUnidadMedidaNavigation?.Nombre ?? "";
                sb.AppendLine($"{codigo};{p.Nombre};{p.Marca};{p.Costo};{p.PorcentajeGanancia};{p.Precio};{categoria};{ubicacion};{p.Stock};{unidad}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportarExcelAsync()
        {
            var productos = await _productRepository.GetAllWithCodigosAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Productos");

            ws.Cells[1, 1].Value = "CodigoBarra";
            ws.Cells[1, 2].Value = "Nombre";
            ws.Cells[1, 3].Value = "Marca";
            ws.Cells[1, 4].Value = "Costo";
            ws.Cells[1, 5].Value = "MargenGanancia";
            ws.Cells[1, 6].Value = "Precio";
            ws.Cells[1, 7].Value = "Categoria";
            ws.Cells[1, 8].Value = "Ubicacion";
            ws.Cells[1, 9].Value = "Stock";
            ws.Cells[1, 10].Value = "UnidadMedida";
            ws.Cells["A1:J1"].Style.Font.Bold = true;

            int row = 2;
            foreach (var p in productos)
            {
                string codigo    = p.CodigoBarras.FirstOrDefault()?.Codigo ?? "";
                string categoria = p.IdCategoriaNavigation?.Categoria ?? "";
                string ubicacion = p.IdUbicacionNavigation != null
                    ? $"Fila {p.IdUbicacionNavigation.Fila} - Sección {p.IdUbicacionNavigation.Seccion} - Nivel {p.IdUbicacionNavigation.Nivel}"
                    : "";
                string unidad    = p.IdUnidadMedidaNavigation?.Nombre ?? "";
                ws.Cells[row, 1].Value = codigo;
                ws.Cells[row, 2].Value = p.Nombre;
                ws.Cells[row, 3].Value = p.Marca;
                ws.Cells[row, 4].Value = p.Costo;
                ws.Cells[row, 5].Value = p.PorcentajeGanancia;
                ws.Cells[row, 6].Value = p.Precio;
                ws.Cells[row, 7].Value = categoria;
                ws.Cells[row, 8].Value = ubicacion;
                ws.Cells[row, 9].Value = p.Stock;
                ws.Cells[row, 10].Value = unidad;
                row++;
            }

            ws.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }

        public byte[] ExportarPlantillaCsv()
        {
            var sb = new StringBuilder();
            sb.AppendLine("CodigoBarra;Nombre;Marca;Costo;MargenGanancia;Categoria;Ubicacion;Stock;UnidadMedida");
            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportarPlantillaExcel()
        {
            using var package = new ExcelPackage();

            // Pre-cargar datos para diccionarios y dropdowns
            List<Categorium> categorias = new();
            List<Ubicacion> ubicaciones = new();
            List<UnidadMedida> unidades = new();
            try { categorias = (await _categoryRepository.GetAll()).ToList(); } catch { }
            try { ubicaciones = await _locationRepository.LocationsQueryable("", true).ToListAsync(); } catch { }
            try { unidades = await _dbContext.UnidadMedidas.AsNoTracking().ToListAsync(); } catch { }

            // ── Hoja 1: Plantilla ──────────────────────────────────────────
            var wsPlantilla = package.Workbook.Worksheets.Add("Plantilla");
            string[] headers = { "CodigoBarra", "Nombre", "Marca", "Costo", "MargenGanancia", "Categoria", "Ubicacion", "Stock", "UnidadMedida" };
            for (int i = 0; i < headers.Length; i++)
                wsPlantilla.Cells[1, i + 1].Value = headers[i];
            wsPlantilla.Cells[$"A1:{(char)('A' + headers.Length - 1)}1"].Style.Font.Bold = true;

            // Dropdowns: Categoria (col F=6), Ubicacion (col G=7), UnidadMedida (col I=9)
            if (categorias.Count > 0)
            {
                var dv = wsPlantilla.DataValidations.AddListValidation("F2:F10001");
                dv.ShowErrorMessage = true;
                dv.ErrorTitle = "Categoría inválida";
                dv.Error = "Seleccioná una categoría de la lista.";
                dv.Formula.ExcelFormula = $"Diccionario_Categorias!$B$2:$B${categorias.Count + 1}";
            }
            if (ubicaciones.Count > 0)
            {
                var dv = wsPlantilla.DataValidations.AddListValidation("G2:G10001");
                dv.ShowErrorMessage = true;
                dv.ErrorTitle = "Ubicación inválida";
                dv.Error = "Seleccioná una ubicación de la lista.";
                dv.Formula.ExcelFormula = $"Diccionario_Ubicaciones!$B$2:$B${ubicaciones.Count + 1}";
            }
            if (unidades.Count > 0)
            {
                var dv = wsPlantilla.DataValidations.AddListValidation("I2:I10001");
                dv.ShowErrorMessage = true;
                dv.ErrorTitle = "Unidad de medida inválida";
                dv.Error = "Seleccioná una unidad de medida de la lista.";
                dv.Formula.ExcelFormula = $"Diccionario_Unidades!$B$2:$B${unidades.Count + 1}";
            }
            wsPlantilla.Cells.AutoFitColumns();

            // ── Hoja 2: Diccionario_Categorias ────────────────────────────
            var wsCat = package.Workbook.Worksheets.Add("Diccionario_Categorias");
            wsCat.Cells[1, 1].Value = "IdCategoria";
            wsCat.Cells[1, 2].Value = "Nombre";
            wsCat.Cells["A1:B1"].Style.Font.Bold = true;
            int catRow = 2;
            foreach (var cat in categorias)
            {
                wsCat.Cells[catRow, 1].Value = cat.IdCategoria;
                wsCat.Cells[catRow, 2].Value = cat.Categoria;
                catRow++;
            }
            wsCat.Cells.AutoFitColumns();

            // ── Hoja 3: Diccionario_Ubicaciones ───────────────────────────
            var wsUbi = package.Workbook.Worksheets.Add("Diccionario_Ubicaciones");
            wsUbi.Cells[1, 1].Value = "IdUbicacion";
            wsUbi.Cells[1, 2].Value = "Detalle";
            wsUbi.Cells["A1:B1"].Style.Font.Bold = true;
            int ubiRow = 2;
            foreach (var u in ubicaciones)
            {
                wsUbi.Cells[ubiRow, 1].Value = u.IdUbicacion;
                wsUbi.Cells[ubiRow, 2].Value = $"Fila {u.Fila} - Sección {u.Seccion} - Nivel {u.Nivel}";
                ubiRow++;
            }
            wsUbi.Cells.AutoFitColumns();

            // ── Hoja 4: Diccionario_Unidades ──────────────────────────────
            var wsUm = package.Workbook.Worksheets.Add("Diccionario_Unidades");
            wsUm.Cells[1, 1].Value = "IdUnidadMedida";
            wsUm.Cells[1, 2].Value = "Nombre";
            wsUm.Cells[1, 3].Value = "Abreviatura";
            wsUm.Cells["A1:C1"].Style.Font.Bold = true;
            int umRow = 2;
            foreach (var u in unidades)
            {
                wsUm.Cells[umRow, 1].Value = u.IdUnidadMedida;
                wsUm.Cells[umRow, 2].Value = u.Nombre;
                wsUm.Cells[umRow, 3].Value = u.Abreviatura;
                umRow++;
            }
            wsUm.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }

        /* Actualización masiva de precios */

        public async Task<Result<byte[]>> DescargarPlantillaPreciosAsync()
        {
            try
            {
                var productos = await _dbContext.Productos
                    .Where(p => p.Activo)
                    .Include(p => p.CodigoBarras)
                    .AsNoTracking()
                    .ToListAsync();

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using var package = new ExcelPackage();
                var ws = package.Workbook.Worksheets.Add("Precios");

                ws.Cells[1, 1].Value = "IdProducto";
                ws.Cells[1, 2].Value = "CodigoBarra";
                ws.Cells[1, 3].Value = "NombreProducto";
                ws.Cells[1, 4].Value = "CostoNeto";
                ws.Cells[1, 5].Value = "MargenGanancia";

                using (var headerRange = ws.Cells[1, 1, 1, 5])
                {
                    headerRange.Style.Font.Bold = true;
                    headerRange.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                    headerRange.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(68, 114, 196));
                    headerRange.Style.Font.Color.SetColor(System.Drawing.Color.White);
                }

                int row = 2;
                foreach (var p in productos)
                {
                    var codigoPrincipal = p.CodigoBarras.FirstOrDefault(c => c.Prinicial == true)?.Codigo
                        ?? p.CodigoBarras.FirstOrDefault()?.Codigo
                        ?? string.Empty;

                    ws.Cells[row, 1].Value = p.IdProducto;
                    ws.Cells[row, 2].Value = codigoPrincipal;
                    ws.Cells[row, 3].Value = p.Nombre;
                    ws.Cells[row, 4].Value = p.Costo;
                    ws.Cells[row, 5].Value = p.PorcentajeGanancia;
                    row++;
                }

                ws.Cells[ws.Dimension.Address].AutoFitColumns();

                return Result<byte[]>.Success(package.GetAsByteArray());
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado al generar plantilla de precios: {Ex}", ex);
                return Result<byte[]>.Failure(ProductErrorCode.error_inesperado);
            }
        }

        public async Task<Result<ActualizarMasivoResultDTO>> ActualizarMasivoManualAsync(ActualizarMasivoManualDTO dto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var ids = dto.Items.Select(i => i.IdProducto).Distinct().ToList();
                var productos = await _dbContext.Productos
                    .Where(p => ids.Contains(p.IdProducto) && p.Activo)
                    .ToListAsync();

                var resultado = new ActualizarMasivoResultDTO();

                foreach (var item in dto.Items)
                {
                    var producto = productos.FirstOrDefault(p => p.IdProducto == item.IdProducto);
                    if (producto == null)
                    {
                        resultado.Ignorados.Add(item.IdProducto);
                        continue;
                    }

                    decimal costoAnterior  = producto.Costo;
                    decimal precioAnterior = producto.Precio ?? 0m;

                    decimal costoConIva = item.CostoNeto * (1 + item.IvaPorcentaje / 100m);

                    producto.Costo = costoConIva;

                    if (item.Margen.HasValue && item.Margen.Value >= 0)
                        producto.PorcentajeGanancia = item.Margen.Value;

                    producto.Precio = producto.Costo * (1 + producto.PorcentajeGanancia / 100m);

                    resultado.Actualizados++;
                    resultado.Detalle.Add(new ActualizarPrecioResultItemDTO
                    {
                        IdProducto     = producto.IdProducto,
                        Nombre         = producto.Nombre,
                        CostoAnterior  = costoAnterior,
                        CostoNuevo     = producto.Costo,
                        PrecioAnterior = precioAnterior,
                        PrecioNuevo    = producto.Precio ?? 0m,
                    });
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await LogAsync(
                    "ACTUALIZACION_PRECIOS_MANUAL",
                    "PRODUCTO",
                    $"{resultado.Actualizados} producto(s) actualizados manualmente",
                    anterior: resultado.Detalle.Select(d => new { d.IdProducto, d.Nombre, Costo = d.CostoAnterior, Precio = d.PrecioAnterior }),
                    nuevo:    resultado.Detalle.Select(d => new { d.IdProducto, d.Nombre, Costo = d.CostoNuevo,    Precio = d.PrecioNuevo })
                );

                return Result<ActualizarMasivoResultDTO>.Success(resultado);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado al actualizar precios masivos (manual): {Ex}", ex);
                return Result<ActualizarMasivoResultDTO>.Failure(ProductErrorCode.error_inesperado);
            }
        }

        public async Task<Result<ActualizarMasivoResultDTO>> ActualizarMasivoExcelAsync(IFormFile file, decimal ivaDefecto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (ext is not (".xlsx" or ".xls" or ".csv"))
                    return Result<ActualizarMasivoResultDTO>.Failure(ProductErrorCode.formato_no_soportado);

                // Parse rows from Excel/CSV
                var rows = ParsePreciosExcel(file);

                var resultado = new ActualizarMasivoResultDTO();

                foreach (var row in rows)
                {
                    var producto = await _dbContext.Productos
                        .Where(p => p.IdProducto == row.IdProducto && p.Activo)
                        .FirstOrDefaultAsync();

                    if (producto == null)
                    {
                        resultado.Ignorados.Add(row.IdProducto);
                        continue;
                    }

                    decimal costoAnterior  = producto.Costo;
                    decimal precioAnterior = producto.Precio ?? 0m;

                    decimal costoConIva = row.CostoNeto * (1 + ivaDefecto / 100m);

                    producto.Costo = costoConIva;

                    if (row.Margen.HasValue && row.Margen.Value >= 0)
                        producto.PorcentajeGanancia = row.Margen.Value;

                    producto.Precio = producto.Costo * (1 + producto.PorcentajeGanancia / 100m);

                    resultado.Actualizados++;
                    resultado.Detalle.Add(new ActualizarPrecioResultItemDTO
                    {
                        IdProducto     = producto.IdProducto,
                        Nombre         = producto.Nombre,
                        CostoAnterior  = costoAnterior,
                        CostoNuevo     = producto.Costo,
                        PrecioAnterior = precioAnterior,
                        PrecioNuevo    = producto.Precio ?? 0m,
                    });
                }

                await _dbContext.SaveChangesAsync();
                await transaction.CommitAsync();

                await LogAsync(
                    "ACTUALIZACION_PRECIOS_EXCEL",
                    "PRODUCTO",
                    $"{resultado.Actualizados} producto(s) actualizados vía Excel",
                    anterior: resultado.Detalle.Select(d => new { d.IdProducto, d.Nombre, Costo = d.CostoAnterior, Precio = d.PrecioAnterior }),
                    nuevo:    resultado.Detalle.Select(d => new { d.IdProducto, d.Nombre, Costo = d.CostoNuevo,    Precio = d.PrecioNuevo })
                );

                return Result<ActualizarMasivoResultDTO>.Success(resultado);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado al actualizar precios masivos (Excel): {Ex}", ex);
                return Result<ActualizarMasivoResultDTO>.Failure(ProductErrorCode.error_inesperado);
            }
        }

        private record PrecioImportRow(int IdProducto, decimal CostoNeto, decimal? Margen);

        private List<PrecioImportRow> ParsePreciosExcel(IFormFile file)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var rows = new List<PrecioImportRow>();

            using var stream = file.OpenReadStream();
            using var package = new ExcelPackage(stream);

            var ws = package.Workbook.Worksheets[0];
            if (ws?.Dimension == null) return rows;

            for (int row = 2; row <= ws.Dimension.Rows; row++)
            {
                // col 1 = IdProducto
                var idText = ws.Cells[row, 1].Text.Trim();
                if (!int.TryParse(idText, out var idProducto) || idProducto <= 0) continue;

                // col 2 = CodigoBarra — ignorar
                // col 3 = NombreProducto — ignorar

                var costoText = ws.Cells[row, 4].Text.Trim();
                if (!decimal.TryParse(costoText, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var costo)) continue;

                decimal? margen = null;
                var margenText = ws.Cells[row, 5].Text.Trim();
                if (!string.IsNullOrWhiteSpace(margenText) &&
                    decimal.TryParse(margenText, System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture, out var m))
                    margen = m;

                rows.Add(new PrecioImportRow(idProducto, costo, margen));
            }

            return rows;
        }

        /* Importacion */

        private List<ProductImportRowDTO> ParseCsv(IFormFile file)
        {
            var rows = new List<ProductImportRowDTO>();

            using var reader = new StreamReader(file.OpenReadStream());
            string header = reader.ReadLine(); // ignorar encabezado

            while (!reader.EndOfStream)
            {
                var line = reader.ReadLine();
                if (string.IsNullOrWhiteSpace(line)) continue;
                var cols = line.Split(';');
                if (string.IsNullOrWhiteSpace(cols[0])) continue;

                rows.Add(new ProductImportRowDTO
                {
                    CodigoBarra    = cols[0],
                    Nombre         = cols[1],
                    Marca          = cols[2],
                    Costo          = cols.Length > 3 && decimal.TryParse(cols[3], out var costo) ? costo : null,
                    MargenGanancia = cols.Length > 4 && decimal.TryParse(cols[4], out var margen) ? margen : null,
                    Categoria      = cols.Length > 5 ? cols[5].Trim() : null,
                    Ubicacion      = cols.Length > 6 ? cols[6].Trim() : null,
                    Stock          = cols.Length > 7 && decimal.TryParse(cols[7], out var s) ? s : null,
                    UnidadMedida   = cols.Length > 8 ? cols[8].Trim() : null,
                });
            }

            return rows;
        }

        private List<ProductImportRowDTO> ParseExcel(IFormFile file)
        {
            var rows = new List<ProductImportRowDTO>();

            using var package = new ExcelPackage(file.OpenReadStream());
            var ws = package.Workbook.Worksheets[0];

            int rowCount = ws.Dimension.Rows;

            for (int row = 2; row <= rowCount; row++)
            {
                string codigoBarra = ws.Cells[row, 1].Text?.Trim() ?? "";
                if (string.IsNullOrWhiteSpace(codigoBarra)) continue;

                int cols = ws.Dimension.Columns;
                rows.Add(new ProductImportRowDTO
                {
                    CodigoBarra    = codigoBarra,
                    Nombre         = ws.Cells[row, 2].Text,
                    Marca          = ws.Cells[row, 3].Text,
                    Costo          = cols >= 4 && decimal.TryParse(ws.Cells[row, 4].Text, out var costo) ? costo : null,
                    MargenGanancia = cols >= 5 && decimal.TryParse(ws.Cells[row, 5].Text, out var margen) ? margen : null,
                    Categoria      = cols >= 6 ? ws.Cells[row, 6].Text.Trim() : null,
                    Ubicacion      = cols >= 7 ? ws.Cells[row, 7].Text.Trim() : null,
                    Stock          = cols >= 8 && decimal.TryParse(ws.Cells[row, 8].Text, out var stk) ? stk : null,
                    UnidadMedida   = cols >= 9 ? ws.Cells[row, 9].Text.Trim() : null,
                });
            }

            return rows;
        }



        public async Task<Result<BulkImportResultDTO>> ImportarProductosAsync(IFormFile file, int idUsuario)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var result = new BulkImportResultDTO();

                var extension = Path.GetExtension(file.FileName).ToLower();
                List<ProductImportRowDTO> rows;

                if (extension == ".csv")
                    rows = ParseCsv(file);
                else if (extension == ".xlsx")
                    rows = ParseExcel(file);
                else
                    throw new Exception("Formato no soportado. Solo CSV o Excel.");

                int lineNumber = 2; // comienza después del encabezado

                // Precargar tablas de referencia para resolver nombres → IDs
                var allCategorias  = (await _categoryRepository.GetAll()).ToList();
                var allUbicaciones = await _locationRepository.LocationsQueryable("", true).ToListAsync();
                var allUnidades    = await _dbContext.UnidadMedidas.AsNoTracking().ToListAsync();

                string FmtUbi(Ubicacion u) => $"Fila {u.Fila} - Sección {u.Seccion} - Nivel {u.Nivel}";

                int? ResolveCategoria(string? nombre) =>
                    string.IsNullOrWhiteSpace(nombre) ? null :
                    allCategorias.FirstOrDefault(c => string.Equals(c.Categoria, nombre, StringComparison.OrdinalIgnoreCase))?.IdCategoria;

                int? ResolveUbicacion(string? detalle) =>
                    string.IsNullOrWhiteSpace(detalle) ? null :
                    allUbicaciones.FirstOrDefault(u => string.Equals(FmtUbi(u), detalle, StringComparison.OrdinalIgnoreCase))?.IdUbicacion;

                int? ResolveUnidad(string? nombre) =>
                    string.IsNullOrWhiteSpace(nombre) ? null :
                    allUnidades.FirstOrDefault(u => string.Equals(u.Nombre, nombre, StringComparison.OrdinalIgnoreCase))?.IdUnidadMedida;

                // set_config para auditoría del trigger de producto
                await _dbContext.Database.SetAuditContextAsync(idUsuario);

                foreach (var row in rows)
                {
                    // Validación IValidatableObject
                    var context = new ValidationContext(row);
                    var validation = new List<ValidationResult>();

                    if (!Validator.TryValidateObject(row, context, validation, true))
                    {
                        result.Errores.Add(new RowErrorDTO
                        {
                            LineNumber = lineNumber,
                            Errors = validation.Select(v => v.ErrorMessage).ToList()
                        });
                        lineNumber++;
                        continue;
                    }

                    // Buscar si existe
                    var existing = await _productRepository.GetByBarcode(row.CodigoBarra);

                    if (existing != null)
                    {
                        // UPDATE
                        existing.Nombre      = string.IsNullOrWhiteSpace(row.Nombre) ? existing.Nombre : row.Nombre;
                        existing.Marca       = string.IsNullOrWhiteSpace(row.Marca) ? existing.Marca : row.Marca;
                        existing.IdCategoria = ResolveCategoria(row.Categoria) ?? existing.IdCategoria;
                        existing.IdUbicacion = ResolveUbicacion(row.Ubicacion) ?? existing.IdUbicacion;
                        var resolvedUnidad   = ResolveUnidad(row.UnidadMedida);
                        if (resolvedUnidad.HasValue)
                            existing.IdUnidadMedida = resolvedUnidad.Value;
                        if (row.Costo.HasValue)
                            existing.Costo = row.Costo.Value;
                        if (row.MargenGanancia.HasValue)
                            existing.PorcentajeGanancia = row.MargenGanancia.Value;
                        existing.Precio = existing.Costo * (1 + existing.PorcentajeGanancia / 100m);

                        await _productRepository.Update(existing);
                        result.ProductosActualizados++;

                        // Sincronización de stock (auditoría de inventario físico)
                        if (row.Stock.HasValue)
                        {
                            decimal stockActual = existing.Stock ?? 0;
                            decimal diferencia = row.Stock.Value - stockActual;

                            if (diferencia > 0)
                            {
                                await _stockMovementService.RegistrarMovimientoAsync(
                                    existing.IdProducto,
                                    TipoMovimientoStockEnum.AjustePositivoManual,
                                    diferencia,
                                    "SINCRONIZACIÓN DE STOCK POR CARGA MASIVA",
                                    idUsuario);
                            }
                            else if (diferencia < 0)
                            {
                                await _stockMovementService.RegistrarMovimientoAsync(
                                    existing.IdProducto,
                                    TipoMovimientoStockEnum.AjusteNegativoManual,
                                    Math.Abs(diferencia),
                                    "SINCRONIZACIÓN DE STOCK POR CARGA MASIVA",
                                    idUsuario);
                            }
                            // Si diferencia == 0 no se hace nada
                        }
                    }
                    else
                    {
                        // INSERT (alta masiva)
                        var defaults = _defaultsOptions.Value;

                        decimal costo  = row.Costo ?? 0m;
                        decimal margen = row.MargenGanancia ?? 30m;
                        decimal precio = costo * (1 + margen / 100m);

                        var nuevo = new Producto
                        {
                            Nombre             = row.Nombre,
                            Marca              = row.Marca,
                            Costo              = costo,
                            PorcentajeGanancia = margen,
                            Precio             = precio,
                            IdCategoria        = ResolveCategoria(row.Categoria) ?? defaults.DefaultCategoryId,
                            IdUbicacion        = ResolveUbicacion(row.Ubicacion) ?? defaults.DefaultLocationId,
                            IdUnidadMedida     = ResolveUnidad(row.UnidadMedida) ?? 1,
                            Activo             = true,
                            CodigoBarras       = new List<CodigoBarra>
                            {
                                new CodigoBarra { Codigo = row.CodigoBarra }
                            }
                        };

                        await _productRepository.Create(nuevo);

                        // Si viene stock inicial > 0, registrarlo en el Ledger
                        if (row.Stock.HasValue && row.Stock.Value > 0)
                        {
                            await _stockMovementService.RegistrarMovimientoAsync(
                                nuevo.IdProducto,
                                TipoMovimientoStockEnum.AjustePositivoManual,
                                row.Stock.Value,
                                "ALTA POR CARGA MASIVA",
                                idUsuario);
                        }

                        result.ProductosCreados++;
                    }

                    lineNumber++;
                }
                await transaction.CommitAsync();

                await LogAsync("IMPORTACION_PRECIOS", "PRODUCTO",
                    $"Importación lista de precios: {result.ProductosCreados} creados, {result.ProductosActualizados} actualizados, {result.Errores.Count} errores",
                    null,
                    new { ProductosCreados = result.ProductosCreados, ProductosActualizados = result.ProductosActualizados, Errores = result.Errores.Count });

                return Result<BulkImportResultDTO>.Success(result);
            }
            catch (System.Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex);
                return Result<BulkImportResultDTO>.Failure(ProductErrorCode.error_inesperado);
            }
        }

    }
}