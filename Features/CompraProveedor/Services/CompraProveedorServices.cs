using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using proyecto_venta_stock.CompraProveedor.PDF;
using proyecto_venta_stock.CompraProveedor.DTO;
using proyecto_venta_stock.CompraProveedor.Message;
using proyecto_venta_stock.CompraProveedor.Repository;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using QuestPDF.Fluent;
using venta_stock_webapi.Features.Audit.Repository;
using venta_stock_webapi.Features.StockMovement.Services;
using venta_stock_webapi.Shared.Extensions;
using venta_stock_webapi.Shared.Identity;
using venta_stock_webapi.Shared.Paged;
using proyecto_venta_stock.Proveedor.ProveedorRepository;
using proyecto_venta_stock.User.UserRepository;
using proyecto_venta_stock.Product.ProductRepository;
using System.Text.Json;

namespace proyecto_venta_stock.CompraProveedor.Services;

public class CompraProveedorServices : ICompraProveedorServices
{
    private readonly ILogger<CompraProveedorServices> _logger;
    private readonly IMapper _mapper;
    private readonly ICompraProveedorRepository _compraRepo;
    private readonly IProveedorRepository _proveedorRepo;
    private readonly IUserRepository _userRepository;
    private readonly IProductRepository _productRepository;
    private readonly VentaStockContext _context;
    private readonly IUserContext _userContext;
    private readonly IStockMovementService _stockMovementService;
    private readonly IAuditRepository _auditRepository;

    public CompraProveedorServices(
        ILogger<CompraProveedorServices> logger,
        IMapper mapper,
        ICompraProveedorRepository compraRepo,
        IProveedorRepository proveedorRepo,
        VentaStockContext context,
        IUserContext userContext,
        IStockMovementService stockMovementService,
        IUserRepository userRepository,
        IProductRepository productRepository,
        IAuditRepository auditRepository)
    {
        _logger = logger;
        _mapper = mapper;
        _compraRepo = compraRepo;
        _context = context;
        _userContext = userContext;
        _stockMovementService = stockMovementService;
        _proveedorRepo = proveedorRepo;
        _userRepository = userRepository;
        _productRepository = productRepository;
        _auditRepository = auditRepository;
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

    private async Task RegistrarMovimientosStockAsync(
        IEnumerable<(int idProducto, decimal cantidad)> lineas,
        TipoMovimientoStockEnum tipo,
        string referencia,
        int idUsuario)
    {
        foreach (var (idProducto, cantidad) in lineas)
            await _stockMovementService.RegistrarMovimientoAsync(idProducto, tipo, cantidad, referencia, idUsuario);
    }

    private static (decimal subtotal, decimal descuento, decimal iva, decimal total) CalcLinea(
        decimal cantidad, decimal precioUnitario, decimal descuentoPorcentaje, decimal ivaPorcentaje)
    {
        var subtotal = cantidad * precioUnitario;
        var descuento = subtotal * (descuentoPorcentaje / 100m);
        var baseIva = subtotal - descuento;
        var iva = baseIva * (ivaPorcentaje / 100m);
        return (subtotal, descuento, iva, baseIva + iva);
    }

    private async Task<(bool flowControl, Result<CompraProveedorResponseDTO> value)> ValidateCreate(CompraProveedorCreateDTO dto)
    {
        if (dto.Detalles == null || dto.Detalles.Count == 0)
            return (false, Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.sin_detalles));

        if (!await _proveedorRepo.Exists(dto.IdProveedor))
            return (false, Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.proveedor_not_found));

        if (dto.IdUsuario > 0 && !await _userRepository.ExistsActive(dto.IdUsuario))
            return (false, Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.usuario_not_found));

        if (!string.IsNullOrWhiteSpace(dto.NumeroComprobante) &&
            await _compraRepo.ExistsByNumeroComprobanteAsync(dto.NumeroComprobante, dto.IdProveedor))
            return (false, Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.numero_comprobante_duplicado));

        var idsProducto = dto.Detalles.Select(d => d.IdProducto).Distinct().ToList();
        if (await _productRepository.QuantityExistsAndActive(idsProducto) != idsProducto.Count)
            return (false, Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.producto_not_found));

        return (true, null);
    }

    private Models.CompraProveedor BuildCompraEntity(CompraProveedorCreateDTO dto)
    {
        decimal subtotal = 0, descuento = 0, iva = 0;
        var detalles = new List<CompraProveedorDetalle>();

        foreach (var d in dto.Detalles)
        {
            var (sub, desc, ivaLine, tot) = CalcLinea(d.Cantidad, d.PrecioUnitario, d.DescuentoPorcentaje, d.IvaPorcentaje);
            subtotal  += sub;
            descuento += desc;
            iva       += ivaLine;

            detalles.Add(new CompraProveedorDetalle
            {
                IdProducto          = d.IdProducto,
                Cantidad            = d.Cantidad,
                PrecioUnitario      = d.PrecioUnitario,
                DescuentoPorcentaje = d.DescuentoPorcentaje,
                IvaPorcentaje       = d.IvaPorcentaje,
                Subtotal            = sub,
                Total               = tot
            });
        }

        return new Models.CompraProveedor
        {
            IdProveedor             = dto.IdProveedor,
            Fecha                   = dto.Fecha,
            FechaVencimiento        = dto.FechaVencimiento,
            TipoComprobante         = dto.TipoComprobante,
            NumeroComprobante       = dto.NumeroComprobante,
            Observacion             = dto.Observacion,
            IdUsuario               = dto.IdUsuario,
            Subtotal                = subtotal,
            DescuentoTotal          = descuento,
            IvaTotal                = iva,
            Total                   = subtotal - descuento + iva,
            Activo                  = true,
            CompraProveedorDetalles = detalles
        };
    }

    private async Task ActualizarCostosProductosAsync(List<CompraProveedorDetalleCreateDTO> detalles)
    {
        var idsProducto = detalles.Select(d => d.IdProducto).Distinct().ToList();
        var productos = await _context.Productos
            .Where(p => idsProducto.Contains(p.IdProducto))
            .ToListAsync();

        foreach (var d in detalles)
        {
            var producto = productos.FirstOrDefault(p => p.IdProducto == d.IdProducto);
            if (producto == null) continue;

            decimal costoReal = d.PrecioUnitario
                * (1 - d.DescuentoPorcentaje / 100m)
                * (1 + d.IvaPorcentaje       / 100m);

            bool margenCambiado = d.MargenAplicado.HasValue;
            bool costoCrecio    = costoReal > producto.Costo;

            if (margenCambiado)
                producto.PorcentajeGanancia = d.MargenAplicado!.Value;

            if (costoCrecio || margenCambiado)
            {
                producto.Costo  = costoReal;
                producto.Precio = producto.Costo * (1 + producto.PorcentajeGanancia / 100m);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task LogCreateAsync(CompraProveedorCreateDTO dto, Models.CompraProveedor compra, CompraProveedorResponseDTO responseDTO)
    {
        string comprobante = string.IsNullOrWhiteSpace(dto.NumeroComprobante)
            ? "(sin número)"
            : $"{dto.TipoComprobante} {dto.NumeroComprobante}".Trim();

        await LogAsync("COMPRA_REGISTRADA", "COMPRA",
            $"Compra registrada: {comprobante} | Proveedor: '{responseDTO.NombreProveedor}' | Total: ${compra.Total:N2} | Ítems: {dto.Detalles.Count}",
            null,
            new
            {
                Comprobante   = comprobante,
                Proveedor     = responseDTO.NombreProveedor,
                Subtotal      = compra.Subtotal,
                Descuento     = compra.DescuentoTotal,
                IVA           = compra.IvaTotal,
                Total         = compra.Total,
                CantidadItems = dto.Detalles.Count
            });
    }

    public async Task<Result<CompraProveedorResponseDTO>> Create(CompraProveedorCreateDTO dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            (bool flowControl, Result<CompraProveedorResponseDTO> value) = await ValidateCreate(dto);

            if (!flowControl) return value;

            var compra = BuildCompraEntity(dto);

            await _compraRepo.CreateAsync(compra);

            await ActualizarCostosProductosAsync(dto.Detalles);

            await RegistrarMovimientosStockAsync(
                dto.Detalles.Select(d => (d.IdProducto, d.Cantidad)),
                TipoMovimientoStockEnum.IngresoCompra,
                $"COMPRA:{compra.IdCompraProveedor}",
                dto.IdUsuario);

            await transaction.CommitAsync();

            var compraCreada = await _compraRepo.GetByIdAsync(compra.IdCompraProveedor);

            var responseDTO  = _mapper.Map<CompraProveedorResponseDTO>(compraCreada);

            await LogCreateAsync(dto, compra, responseDTO);

            return Result<CompraProveedorResponseDTO>.Success(responseDTO);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError("Error inesperado al crear compra: {Ex}", ex);
            return Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    public async Task<Result<PagedList<CompraProveedorDetailResponseDTO>>> GetPaged(
        int pageIndex, int pageSize, string? search, bool? activo, DateOnly? fechaDesde, DateOnly? fechaHasta)
    {
        try
        {
            var paged = await _compraRepo.GetPagedWithDetailsAsync(pageIndex, pageSize, search, activo, fechaDesde, fechaHasta);

            var dtos = _mapper.Map<List<CompraProveedorDetailResponseDTO>>(paged.Items);

            var result = new PagedList<CompraProveedorDetailResponseDTO>(dtos, paged.TotalCount, pageIndex, pageSize);

            return Result<PagedList<CompraProveedorDetailResponseDTO>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al obtener compras paginadas: {Ex}", ex);

            return Result<PagedList<CompraProveedorDetailResponseDTO>>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    public async Task<Result<PagedList<CompraProveedorDetailResponseDTO>>> GetPagedByProveedor(
        int idProveedor, int pageIndex, int pageSize, bool? activo, DateOnly? fechaDesde, DateOnly? fechaHasta)
    {
        try
        {
            var paged = await _compraRepo.GetPagedByProveedorAsync(idProveedor, pageIndex, pageSize, activo, fechaDesde, fechaHasta);

            var dtos = _mapper.Map<List<CompraProveedorDetailResponseDTO>>(paged.Items);

            var result = new PagedList<CompraProveedorDetailResponseDTO>(dtos, paged.TotalCount, pageIndex, pageSize);

            return Result<PagedList<CompraProveedorDetailResponseDTO>>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al obtener compras por proveedor: {Ex}", ex);

            return Result<PagedList<CompraProveedorDetailResponseDTO>>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    public async Task<Result<CompraProveedorDetailResponseDTO>> GetById(int idCompraProveedor)
    {
        try
        {
            var compra = await _compraRepo.GetByIdWithDetailsAsync(idCompraProveedor);

            if (compra is null)
                return Result<CompraProveedorDetailResponseDTO>.Failure(CompraProveedorErrorCode.compra_not_found);

            var dto = _mapper.Map<CompraProveedorDetailResponseDTO>(compra);
            return Result<CompraProveedorDetailResponseDTO>.Success(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al obtener compra: {Ex}", ex);
            return Result<CompraProveedorDetailResponseDTO>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    private (bool flowControl, Result<bool> value) ValidateAnular(Models.CompraProveedor? compra)
    {
        if (compra is null)
            return (false, Result<bool>.Failure(CompraProveedorErrorCode.compra_not_found));

        if (!compra.Activo)
            return (false, Result<bool>.Failure(CompraProveedorErrorCode.compra_ya_inactiva));

        return (true, null);
    }

    private async Task LogAnulacionAsync(string comprobante, string nombreProveedor, decimal total, string motivo)
    {
        await LogAsync("COMPRA_ANULADA", "COMPRA",
            $"Compra anulada: {comprobante} | Proveedor: '{nombreProveedor}' | Total: ${total:N2} | Motivo: {motivo}",
            new { Comprobante = comprobante, Proveedor = nombreProveedor, Total = total, Activo = true },
            new { Activo = false, Motivo = motivo });
    }

    public async Task<Result<bool>> Anular(int idCompraProveedor, AnulacionCompraDTO dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await _context.Database.SetAuditContextAsync(_userContext);

            var compra = await _compraRepo.GetByIdForUpdateAsync(idCompraProveedor);

            (bool flowControl, Result<bool> value) = ValidateAnular(compra);

            if (!flowControl) return value;

            var proveedor      = await _proveedorRepo.GetById(compra!.IdProveedor);

            string nombreProveedor = proveedor?.Proveedor1 ?? $"Proveedor #{compra.IdProveedor}";

            string comprobante = string.IsNullOrWhiteSpace(compra.NumeroComprobante)
                ? "(sin número)"
                : $"{compra.TipoComprobante} {compra.NumeroComprobante}".Trim();


            await RegistrarMovimientosStockAsync(
                compra.CompraProveedorDetalles.Select(d => (d.IdProducto, -d.Cantidad)),
                TipoMovimientoStockEnum.EgresoAnulacionCompra,
                $"ANULACION:COMPRA:{idCompraProveedor}",
                _userContext.UserId);

            compra.Observacion = string.IsNullOrWhiteSpace(compra.Observacion)
                ? $"ANULADO: {dto.Motivo}"
                : $"{compra.Observacion} - ANULADO: {dto.Motivo}";
                
            compra.Activo = false;

            await _compraRepo.SaveChangesAsync();
            await transaction.CommitAsync();

            await LogAnulacionAsync(comprobante, nombreProveedor, compra.Total, dto.Motivo);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError("Error inesperado al anular compra: {Ex}", ex);
            return Result<bool>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    public async Task<Result<byte[]>> ExportarExcelAsync(DateOnly? fechaDesde, DateOnly? fechaHasta)
    {
        try
        {
            var compras = await _compraRepo.GetAllWithDetailsAsync(fechaDesde, fechaHasta);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();

    
            var wsCompras = package.Workbook.Worksheets.Add("Compras");
            var headersCompras = new[]
            {
                "ID", "Proveedor", "Fecha", "Vencimiento", "Tipo Comprobante",
                "Nro. Comprobante", "Subtotal", "Descuento", "IVA", "Total", "Activo"
            };
            EstiloEncabezado(wsCompras, headersCompras);

            for (int i = 0; i < compras.Count; i++)
            {
                var c = compras[i];
                var row = i + 2;
                wsCompras.Cells[row, 1].Value = c.IdCompraProveedor;
                wsCompras.Cells[row, 2].Value = c.IdProveedorNavigation?.Proveedor1;
                wsCompras.Cells[row, 3].Value = c.Fecha.ToString("dd/MM/yyyy");
                wsCompras.Cells[row, 4].Value = c.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "";
                wsCompras.Cells[row, 5].Value = c.TipoComprobante;
                wsCompras.Cells[row, 6].Value = c.NumeroComprobante;
                wsCompras.Cells[row, 7].Value = c.Subtotal;
                wsCompras.Cells[row, 8].Value = c.DescuentoTotal;
                wsCompras.Cells[row, 9].Value = c.IvaTotal;
                wsCompras.Cells[row, 10].Value = c.Total;
                wsCompras.Cells[row, 11].Value = c.Activo ? "Si" : "No";
            }
            wsCompras.Cells[wsCompras.Dimension.Address].AutoFitColumns();

            // Hoja 2: Detalle por producto (una fila por producto por compra) â”€â”€
            var wsDetalles = package.Workbook.Worksheets.Add("Detalle por Producto");
            var headersDetalles = new[]
            {
                "ID Compra", "Proveedor", "Fecha", "Nro. Comprobante",
                "Producto", "Cantidad", "Unidad", "Precio c/u", "Desc %", "IVA %",
                "Total sin IVA", "Total c/IVA"
            };
            EstiloEncabezado(wsDetalles, headersDetalles);

            int detalleRow = 2;
            foreach (var c in compras)
            {
                foreach (var d in c.CompraProveedorDetalles)
                {
                    var baseIva = d.Subtotal - (d.Subtotal * (d.DescuentoPorcentaje / 100m));
                    var unidad = d.IdProductoNavigation?.IdUnidadMedidaNavigation?.Abreviatura ?? "";
                    wsDetalles.Cells[detalleRow, 1].Value = c.IdCompraProveedor;
                    wsDetalles.Cells[detalleRow, 2].Value = c.IdProveedorNavigation?.Proveedor1;
                    wsDetalles.Cells[detalleRow, 3].Value = c.Fecha.ToString("dd/MM/yyyy");
                    wsDetalles.Cells[detalleRow, 4].Value = c.NumeroComprobante;
                    wsDetalles.Cells[detalleRow, 5].Value = d.IdProductoNavigation?.Nombre;
                    wsDetalles.Cells[detalleRow, 6].Value = d.Cantidad;
                    wsDetalles.Cells[detalleRow, 7].Value = unidad;
                    wsDetalles.Cells[detalleRow, 8].Value = d.PrecioUnitario;
                    wsDetalles.Cells[detalleRow, 9].Value = d.DescuentoPorcentaje;
                    wsDetalles.Cells[detalleRow, 10].Value = d.IvaPorcentaje;
                    wsDetalles.Cells[detalleRow, 11].Value = baseIva;
                    wsDetalles.Cells[detalleRow, 12].Value = d.Total;
                    detalleRow++;
                }

                // Fila resumen de compra
                var summaryRow = detalleRow;
                wsDetalles.Cells[summaryRow, 4].Value = $"Subtotal compra #{c.IdCompraProveedor}";
                wsDetalles.Cells[summaryRow, 4].Style.Font.Bold = true;
                wsDetalles.Cells[summaryRow, 11].Value = c.Subtotal - c.DescuentoTotal;
                wsDetalles.Cells[summaryRow, 11].Style.Font.Bold = true;
                wsDetalles.Cells[summaryRow, 12].Value = c.Total;
                wsDetalles.Cells[summaryRow, 12].Style.Font.Bold = true;
                wsDetalles.Cells[summaryRow, 1, summaryRow, 12].Style.Fill.PatternType = ExcelFillStyle.Solid;
                wsDetalles.Cells[summaryRow, 1, summaryRow, 12].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(220, 230, 245));
                detalleRow++;
            }
            if (wsDetalles.Dimension != null)
                wsDetalles.Cells[wsDetalles.Dimension.Address].AutoFitColumns();

            return Result<byte[]>.Success(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al exportar compras: {Ex}", ex);
            return Result<byte[]>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    // ── EXPORT PDF LISTADO DE COMPRAS ──────────────────────────────────────────────
    public async Task<Result<byte[]>> ExportarPdfAsync(DateOnly? fechaDesde, DateOnly? fechaHasta)
    {
        try
        {
            var comprasReporte = (await _compraRepo.GetAllWithDetailsAsync(fechaDesde, fechaHasta))
                .OrderByDescending(c => c.Fecha)
                .ThenByDescending(c => c.IdCompraProveedor)
                .ToList();

            var filtroTextoReporte = fechaDesde.HasValue || fechaHasta.HasValue
                ? $"Periodo: {(fechaDesde.HasValue ? fechaDesde.Value.ToString("dd/MM/yyyy") : "inicio")} - {(fechaHasta.HasValue ? fechaHasta.Value.ToString("dd/MM/yyyy") : "hoy")}" 
                : "Todas las compras";

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var document = new CompraProveedorReportDocument(comprasReporte, filtroTextoReporte, DateTime.Now);

            return Result<byte[]>.Success(document.GeneratePdf());
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al exportar compras a PDF: {Ex}", ex);
            return Result<byte[]>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    // ── EXPORT COMPRA INDIVIDUAL ──────────────────────────────────────────────

    public async Task<Result<byte[]>> ExportarCompraExcelAsync(int idCompraProveedor)
    {
        try
        {
            var compra = await _compraRepo.GetByIdWithDetailsAsync(idCompraProveedor);
            if (compra == null)
                return Result<byte[]>.Failure(CompraProveedorErrorCode.compra_not_found);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();
            var ws = package.Workbook.Worksheets.Add("Compra");

            // Encabezado de compra
            ws.Cells[1, 1].Value = "Compra #";        ws.Cells[1, 2].Value = compra.IdCompraProveedor;
            ws.Cells[2, 1].Value = "Proveedor";        ws.Cells[2, 2].Value = compra.IdProveedorNavigation?.Proveedor1;
            ws.Cells[3, 1].Value = "Fecha";            ws.Cells[3, 2].Value = compra.Fecha.ToString("dd/MM/yyyy");
            ws.Cells[4, 1].Value = "Vencimiento";      ws.Cells[4, 2].Value = compra.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "-";
            ws.Cells[5, 1].Value = "Comprobante";      ws.Cells[5, 2].Value = $"{compra.TipoComprobante} {compra.NumeroComprobante}".Trim();
            ws.Cells[6, 1].Value = "Observación";      ws.Cells[6, 2].Value = compra.Observacion;
            ws.Cells[7, 1].Value = "Estado";           ws.Cells[7, 2].Value = compra.Activo ? "Activa" : "Anulada";

            for (int r = 1; r <= 7; r++)
                ws.Cells[r, 1].Style.Font.Bold = true;

            // Tabla de productos
            var headers = new[] { "Producto", "Cantidad", "Unidad", "Precio c/u", "Desc %", "IVA %", "Total s/IVA", "Total c/IVA" };
            EstiloEncabezado(ws, headers, startRow: 9);

            int row = 10;
            foreach (var d in compra.CompraProveedorDetalles)
            {
                var baseIva = d.Subtotal - (d.Subtotal * (d.DescuentoPorcentaje / 100m));
                ws.Cells[row, 1].Value = d.IdProductoNavigation?.Nombre;
                ws.Cells[row, 2].Value = d.Cantidad;
                ws.Cells[row, 3].Value = d.IdProductoNavigation?.IdUnidadMedidaNavigation?.Abreviatura ?? "";
                ws.Cells[row, 4].Value = d.PrecioUnitario;
                ws.Cells[row, 5].Value = d.DescuentoPorcentaje;
                ws.Cells[row, 6].Value = d.IvaPorcentaje;
                ws.Cells[row, 7].Value = baseIva;
                ws.Cells[row, 8].Value = d.Total;
                row++;
            }

            // Fila de totales
            ws.Cells[row, 7].Value = compra.Subtotal - compra.DescuentoTotal;
            ws.Cells[row, 8].Value = compra.Total;
            for (int c = 1; c <= 8; c++)
            {
                ws.Cells[row, c].Style.Font.Bold = true;
                ws.Cells[row, c].Style.Fill.PatternType = ExcelFillStyle.Solid;
                ws.Cells[row, c].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(220, 230, 245));
            }
            ws.Cells[row, 6].Value = "TOTAL:";
            ws.Cells[row, 6].Style.HorizontalAlignment = OfficeOpenXml.Style.ExcelHorizontalAlignment.Right;

            ws.Cells[ws.Dimension.Address].AutoFitColumns();
            return Result<byte[]>.Success(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al exportar compra individual a Excel: {Ex}", ex);
            return Result<byte[]>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    public async Task<Result<byte[]>> ExportarCompraPdfAsync(int idCompraProveedor)
    {
        try
        {
            var compra = await _compraRepo.GetByIdWithDetailsAsync(idCompraProveedor);

            if (compra is null)
                return Result<byte[]>.Failure(CompraProveedorErrorCode.compra_not_found);

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var document = new CompraProveedorReportDocument(
                new List<Models.CompraProveedor> { compra },
                $"Compra #{compra.IdCompraProveedor}",
                DateTime.Now);

            return Result<byte[]>.Success(document.GeneratePdf());
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al exportar compra individual a PDF: {Ex}", ex);

            return Result<byte[]>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    // ── EXPORT COMPRAS POR PROVEEDOR ──────────────────────────────────────────

    public async Task<Result<byte[]>> ExportarComprasPorProveedorExcelAsync(int idProveedor, DateOnly? fechaDesde, DateOnly? fechaHasta)
    {
        try
        {
            var compras = await _compraRepo.GetAllByProveedorWithDetailsAsync(idProveedor, fechaDesde, fechaHasta);

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();

            var wsCompras = package.Workbook.Worksheets.Add("Compras");
            var headersCompras = new[] { "ID", "Fecha", "Vencimiento", "Comprobante", "Subtotal", "Descuento", "IVA", "Total", "Estado" };
            EstiloEncabezado(wsCompras, headersCompras);

            for (int i = 0; i < compras.Count; i++)
            {
                var c = compras[i];
                int r = i + 2;
                wsCompras.Cells[r, 1].Value = c.IdCompraProveedor;
                wsCompras.Cells[r, 2].Value = c.Fecha.ToString("dd/MM/yyyy");
                wsCompras.Cells[r, 3].Value = c.FechaVencimiento?.ToString("dd/MM/yyyy") ?? "";
                wsCompras.Cells[r, 4].Value = $"{c.TipoComprobante} {c.NumeroComprobante}".Trim();
                wsCompras.Cells[r, 5].Value = c.Subtotal;
                wsCompras.Cells[r, 6].Value = c.DescuentoTotal;
                wsCompras.Cells[r, 7].Value = c.IvaTotal;
                wsCompras.Cells[r, 8].Value = c.Total;
                wsCompras.Cells[r, 9].Value = c.Activo ? "Activa" : "Anulada";
            }
            wsCompras.Cells[wsCompras.Dimension?.Address ?? "A1"].AutoFitColumns();

            var wsDetalles = package.Workbook.Worksheets.Add("Detalle por Producto");
            var headersDetalles = new[] { "ID Compra", "Fecha", "Nro. Comprobante", "Producto", "Cantidad", "Unidad", "Precio c/u", "Desc %", "IVA %", "Total s/IVA", "Total c/IVA" };
            EstiloEncabezado(wsDetalles, headersDetalles);

            int detalleRow = 2;
            foreach (var c in compras)
            {
                foreach (var d in c.CompraProveedorDetalles)
                {
                    var baseIva = d.Subtotal - (d.Subtotal * (d.DescuentoPorcentaje / 100m));
                    var unidad = d.IdProductoNavigation?.IdUnidadMedidaNavigation?.Abreviatura ?? "";
                    wsDetalles.Cells[detalleRow, 1].Value = c.IdCompraProveedor;
                    wsDetalles.Cells[detalleRow, 2].Value = c.Fecha.ToString("dd/MM/yyyy");
                    wsDetalles.Cells[detalleRow, 3].Value = c.NumeroComprobante;
                    wsDetalles.Cells[detalleRow, 4].Value = d.IdProductoNavigation?.Nombre;
                    wsDetalles.Cells[detalleRow, 5].Value = d.Cantidad;
                    wsDetalles.Cells[detalleRow, 6].Value = unidad;
                    wsDetalles.Cells[detalleRow, 7].Value = d.PrecioUnitario;
                    wsDetalles.Cells[detalleRow, 8].Value = d.DescuentoPorcentaje;
                    wsDetalles.Cells[detalleRow, 9].Value = d.IvaPorcentaje;
                    wsDetalles.Cells[detalleRow, 10].Value = baseIva;
                    wsDetalles.Cells[detalleRow, 11].Value = d.Total;
                    detalleRow++;
                }
                var sr = detalleRow;
                wsDetalles.Cells[sr, 3].Value = $"Subtotal compra #{c.IdCompraProveedor}";
                wsDetalles.Cells[sr, 3].Style.Font.Bold = true;
                wsDetalles.Cells[sr, 10].Value = c.Subtotal - c.DescuentoTotal;
                wsDetalles.Cells[sr, 10].Style.Font.Bold = true;
                wsDetalles.Cells[sr, 11].Value = c.Total;
                wsDetalles.Cells[sr, 11].Style.Font.Bold = true;
                wsDetalles.Cells[sr, 1, sr, 11].Style.Fill.PatternType = ExcelFillStyle.Solid;
                wsDetalles.Cells[sr, 1, sr, 11].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(220, 230, 245));
                detalleRow++;
            }
            if (wsDetalles.Dimension != null)
                wsDetalles.Cells[wsDetalles.Dimension.Address].AutoFitColumns();

            return Result<byte[]>.Success(package.GetAsByteArray());
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al exportar compras del proveedor a Excel: {Ex}", ex);
            return Result<byte[]>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    public async Task<Result<byte[]>> ExportarComprasPorProveedorPdfAsync(int idProveedor, DateOnly? fechaDesde, DateOnly? fechaHasta)
    {
        try
        {
            var compras = await _compraRepo.GetAllByProveedorWithDetailsAsync(idProveedor, fechaDesde, fechaHasta);
            var proveedor = compras.FirstOrDefault()?.IdProveedorNavigation?.Proveedor1
                ?? (await _compraRepo.GetAllByProveedorWithDetailsAsync(idProveedor, null, null))
                    .FirstOrDefault()?.IdProveedorNavigation?.Proveedor1
                ?? $"Proveedor #{idProveedor}";

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
            var filtroTexto = fechaDesde.HasValue || fechaHasta.HasValue
                ? $"Proveedor: {proveedor} — Período: {(fechaDesde.HasValue ? fechaDesde.Value.ToString("dd/MM/yyyy") : "inicio")} - {(fechaHasta.HasValue ? fechaHasta.Value.ToString("dd/MM/yyyy") : "hoy")}"
                : $"Proveedor: {proveedor} — Todas las compras";

            var document = new CompraProveedorReportDocument(compras, filtroTexto, DateTime.Now);
            return Result<byte[]>.Success(document.GeneratePdf());
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al exportar compras del proveedor a PDF: {Ex}", ex);
            return Result<byte[]>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    private static void EstiloEncabezado(ExcelWorksheet ws, string[] headers, int startRow = 1)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cells[startRow, i + 1].Value = headers[i];
            ws.Cells[startRow, i + 1].Style.Font.Bold = true;
            ws.Cells[startRow, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[startRow, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(68, 114, 196));
            ws.Cells[startRow, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
        }
    }
}

