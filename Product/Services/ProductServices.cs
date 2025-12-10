using AutoMapper;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Product.DTO;
using proyecto_venta_stock.Product.ProductRepository;
using proyecto_venta_stock.Category.CategoryRepository;
using proyecto_venta_stock.Location.LocationRepository;
using venta_stock_webapi.Shared.Paged;
using OfficeOpenXml;
using System.Text;
namespace proyecto_venta_stock.Product.Services
{
    public class ProductServices : IProductServices
    {
        private readonly ILogger<ProductServices> _logger;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly ILocationRepository _locationRepository;
        private readonly IMapper _mapper;
        public ProductServices(IProductRepository productRepository, ILogger<ProductServices> logger, IMapper mapper, ICategoryRepository categoryRepository, ILocationRepository locationRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _locationRepository = locationRepository;
            _logger = logger;
            _mapper = mapper;
        }
        public async Task<Result<bool>> Create(ProductDTO productDTO)
        {
            try
            {
                bool productExists = await _productRepository.Exists(productDTO.Nombre, productDTO.Marca);


                if (productExists) return Result<bool>.Failure(ProductErrorCode.product_name_in_use);

                if (productDTO.IdCategoria == null || !await _categoryRepository.ExistsById(productDTO.IdCategoria.Value))
                    return Result<bool>.Failure(ProductErrorCode.categoria_invalida);

                var ubic = productDTO.IdUbicacion == null
                ? null
                : await _locationRepository.GetByIdAsync(productDTO.IdUbicacion.Value);
                if (ubic == null)
                    return Result<bool>.Failure(ProductErrorCode.ubicacion_invalida);

                foreach (var cb in productDTO.CodigoBarras)
                {
                    if (await _productRepository.CodigoBarraExists(cb))
                        return Result<bool>.Failure(ProductErrorCode.error_inesperado); // código de barra duplicado
                }

                var product = _mapper.Map<Producto>(productDTO);

                await _productRepository.Create(product);

                return Result<bool>.Succes();
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure(ProductErrorCode.error_inesperado);
            }
        }

        public async Task<Result<bool>> Update(ProductDTO productDTO)
        {
            try
            {
                // Verificar si el producto existe
                var existingProduct = await _productRepository.GetById(productDTO.IdProducto);
                if (existingProduct == null) return Result<bool>.Failure(ProductErrorCode.product_not_found);

                // Verificar si el nuevo nombre y marca ya están en uso por otro producto
                bool productExists = await _productRepository.Exists(productDTO.Nombre, productDTO.Marca);
                if (productExists && (existingProduct.Nombre != productDTO.Nombre || existingProduct.Marca != productDTO.Marca))
                    return Result<bool>.Failure(ProductErrorCode.product_name_in_use);

                if (productDTO.IdCategoria == null || !await _categoryRepository.ExistsById(productDTO.IdCategoria.Value))
                    return Result<bool>.Failure(ProductErrorCode.categoria_invalida);

                var ubic = productDTO.IdUbicacion == null
                   ? null
                   : await _locationRepository.GetByIdAsync(productDTO.IdUbicacion.Value);
                if (ubic == null)
                    return Result<bool>.Failure(ProductErrorCode.ubicacion_invalida);

                foreach (var cb in productDTO.CodigoBarras)
                {
                    if (await _productRepository.CodigoBarraExists(cb) && !existingProduct.CodigoBarras.Any(e => e.Codigo == cb.Codigo))
                        return Result<bool>.Failure(ProductErrorCode.error_inesperado); // código de barra duplicado
                }

                // Mapear los cambios al producto existente
                _mapper.Map(productDTO, existingProduct);

                await _productRepository.Update(existingProduct);

                return Result<bool>.Succes();
            }
            catch (System.Exception ex)
            {
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
                return Result<List<ProductDTO>>.Succes(dtos);
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
                return Result<List<ProductDetailDTO>>.Succes(dtos);
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
                return Result<ProductDTO>.Succes(dto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<ProductDTO>.Failure(ProductErrorCode.error_inesperado);
            }
        }

        public async Task<Result<bool>> Delete(int idProducto)
        {
            try
            {
                var existing = await _productRepository.GetById(idProducto);
                if (existing == null)
                    return Result<bool>.Failure(ProductErrorCode.product_not_found);

                await _productRepository.Delete(existing);
                return Result<bool>.Succes(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex);
                return Result<bool>.Failure(ProductErrorCode.error_inesperado);
            }
        }
        public async Task<Result<bool>> ToggleEstado(int idProducto)
        {
            try
            {
                var existing = await _productRepository.GetById(idProducto);
                if (existing == null)
                    return Result<bool>.Failure(ProductErrorCode.product_not_found);

                existing.Activo = !existing.Activo;  // 👈 invierte el estado
                await _productRepository.Update(existing);

                return Result<bool>.Succes(true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex);
                return Result<bool>.Failure(ProductErrorCode.error_inesperado);
            }
        }

        public async Task<Result<PagedList<ProductDetailDTO>>> GetAllWithCategoryAndLocationPaged(
     int pageIndex,
     int pageSize,
     bool? activo = true,
     string? search = null)
        {
            try
            {
                var query = _productRepository.QueryAllWithCategoryAndLocation(activo);

                // aplicar búsqueda si hay
                if (!string.IsNullOrWhiteSpace(search))
                {
                    var lower = search.ToLower();
                    query = query.Where(p =>
                        p.Nombre.ToLower().Contains(lower) ||
                        p.Marca.ToLower().Contains(lower) ||
                        (p.Descripcion != null && p.Descripcion.ToLower().Contains(lower))
                    );
                }

                var paged = await PagedList<Producto>.CreateAsync(query, pageIndex, pageSize);
                var dtoItems = _mapper.Map<List<ProductDetailDTO>>(paged.Items);

                var dtoPaged = new PagedList<ProductDetailDTO>(
                    dtoItems, paged.TotalCount, paged.PagedIndex, paged.PageSize);

                return Result<PagedList<ProductDetailDTO>>.Succes(dtoPaged);
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

            // Encabezado
            sb.AppendLine("CodigoBarra;Nombre;Marca;Precio;IdCategoria;IdUbicacion");

            foreach (var p in productos)
            {
                string codigo = p.CodigoBarras.FirstOrDefault()?.Codigo ?? "";

                sb.AppendLine($"{codigo};{p.Nombre};{p.Marca};{p.Precio};{p.IdCategoria};{p.IdUbicacion}");
            }

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public async Task<byte[]> ExportarExcelAsync()
        {
            var productos = await _productRepository.GetAllWithCodigosAsync();

            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Productos");

            // Encabezados
            ws.Cells[1, 1].Value = "CodigoBarra";
            ws.Cells[1, 2].Value = "Nombre";
            ws.Cells[1, 3].Value = "Marca";
            ws.Cells[1, 4].Value = "Precio";
            ws.Cells[1, 5].Value = "IdCategoria";
            ws.Cells[1, 6].Value = "IdUbicacion";

            int row = 2;

            foreach (var p in productos)
            {
                string codigo = p.CodigoBarras.FirstOrDefault()?.Codigo ?? "";

                ws.Cells[row, 1].Value = codigo;
                ws.Cells[row, 2].Value = p.Nombre;
                ws.Cells[row, 3].Value = p.Marca;
                ws.Cells[row, 4].Value = p.Precio;
                ws.Cells[row, 5].Value = p.IdCategoria;
                ws.Cells[row, 6].Value = p.IdUbicacion;

                row++;
            }

            ws.Cells.AutoFitColumns();

            return await package.GetAsByteArrayAsync();
        }

        public byte[] ExportarPlantillaCsv()
        {
            var sb = new StringBuilder();

            // Encabezado EXACTO de la plantilla
            sb.AppendLine("CodigoBarra;Nombre;Marca;Precio;IdCategoria;IdUbicacion");

            // Podés dejar el archivo vacío o incluir un ejemplo comentado
            // Ejemplo descomentable por si querés:
            // sb.AppendLine("7796584001234;Martillo;Acme;3500;1;2");

            return Encoding.UTF8.GetBytes(sb.ToString());
        }

        public byte[] ExportarPlantillaExcel()
        {
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Plantilla");

            ws.Cells[1, 1].Value = "CodigoBarra";
            ws.Cells[1, 2].Value = "Nombre";
            ws.Cells[1, 3].Value = "Marca";
            ws.Cells[1, 4].Value = "Precio";
            ws.Cells[1, 5].Value = "IdCategoria";
            ws.Cells[1, 6].Value = "IdUbicacion";

            ws.Cells["A1:F1"].Style.Font.Bold = true;
            ws.Cells.AutoFitColumns();

            return package.GetAsByteArray();
        }


    }
}