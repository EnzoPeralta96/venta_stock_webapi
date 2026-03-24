using AutoMapper;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using proyecto_venta_stock.CompraProveedor.DTO;
using proyecto_venta_stock.CompraProveedor.Message;
using proyecto_venta_stock.CompraProveedor.Repository;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Features.StockMovement.Services;
using venta_stock_webapi.Shared.Identity;

namespace proyecto_venta_stock.CompraProveedor.Services;

public class CompraProveedorServices : ICompraProveedorServices
{
    private readonly ILogger<CompraProveedorServices> _logger;
    private readonly IMapper _mapper;
    private readonly ICompraProveedorRepository _compraRepo;
    private readonly VentaStockContext _context;
    private readonly IUserContext _userContext;
    private readonly IStockMovementService _stockMovementService;

    public CompraProveedorServices(
        ILogger<CompraProveedorServices> logger,
        IMapper mapper,
        ICompraProveedorRepository compraRepo,
        VentaStockContext context,
        IUserContext userContext,
        IStockMovementService stockMovementService)
    {
        _logger = logger;
        _mapper = mapper;
        _compraRepo = compraRepo;
        _context = context;
        _userContext = userContext;
        _stockMovementService = stockMovementService;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Setea las variables de sesión de PostgreSQL requeridas por los triggers de auditoría.
    /// Debe llamarse antes de cualquier SQL raw (ExecuteSqlRawAsync / ExecuteUpdateAsync)
    /// que no esté precedido por un SaveChangesAsync (el cual dispara el AuditSessionInterceptor).
    /// </summary>
    private async Task SetAuditContextAsync()
    {
        await _context.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.user_id', {0}::text, true);", _userContext.UserId);
        await _context.Database.ExecuteSqlRawAsync(
            "SELECT set_config('app.username', {0}, true);", _userContext.UserName ?? "");
    }

    // ── Calcular línea de detalle ──────────────────────────────────────────────

    private static (decimal subtotal, decimal descuento, decimal iva, decimal total) CalcLinea(
        decimal cantidad, decimal precioUnitario, decimal descuentoPorcentaje, decimal ivaPorcentaje)
    {
        var subtotal = cantidad * precioUnitario;
        var descuento = subtotal * (descuentoPorcentaje / 100m);
        var baseIva = subtotal - descuento;
        var iva = baseIva * (ivaPorcentaje / 100m);
        return (subtotal, descuento, iva, baseIva + iva);
    }

    // ── CREATE ────────────────────────────────────────────────────────────────

    public async Task<Result<CompraProveedorResponseDTO>> Create(CompraProveedorCreateDTO dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            if (dto.Detalles == null || dto.Detalles.Count == 0)
                return Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.sin_detalles);

            // Validar proveedor
            var proveedorExiste = await _context.Proveedors
                .AnyAsync(p => p.IdProveedor == dto.IdProveedor && p.Activo);
            if (!proveedorExiste)
                return Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.proveedor_not_found);

            // Validar usuario
            if (dto.IdUsuario > 0)
            {
                var usuarioExiste = await _context.Usuarios
                    .AnyAsync(u => u.IdUsuario == dto.IdUsuario && u.FechaBaja == null);
                if (!usuarioExiste)
                    return Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.usuario_not_found);
            }

            // Validar número de comprobante duplicado
            if (!string.IsNullOrWhiteSpace(dto.NumeroComprobante))
            {
                var duplicado = await _compraRepo.ExistsByNumeroComprobanteAsync(
                    dto.NumeroComprobante, dto.IdProveedor);
                if (duplicado)
                    return Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.numero_comprobante_duplicado);
            }

            // Validar que los productos existan y estén activos
            var idsProducto = dto.Detalles.Select(d => d.IdProducto).Distinct().ToList();
            var productosExistentes = await _context.Productos
                .Where(p => idsProducto.Contains(p.IdProducto) && p.Activo)
                .AsNoTracking()
                .CountAsync();

            if (productosExistentes != idsProducto.Count)
                return Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.producto_not_found);

            // Construir detalles y calcular totales
            decimal subtotalTotal = 0, descuentoTotal = 0, ivaTotal = 0;
            var detalles = new List<Models.CompraProveedorDetalle>();

            foreach (var d in dto.Detalles)
            {
                var (sub, desc, iva, tot) = CalcLinea(d.Cantidad, d.PrecioUnitario, d.DescuentoPorcentaje, d.IvaPorcentaje);
                subtotalTotal += sub;
                descuentoTotal += desc;
                ivaTotal += iva;

                detalles.Add(new Models.CompraProveedorDetalle
                {
                    IdProducto = d.IdProducto,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    DescuentoPorcentaje = d.DescuentoPorcentaje,
                    IvaPorcentaje = d.IvaPorcentaje,
                    Subtotal = sub,
                    Total = tot
                });
            }

            var compra = new Models.CompraProveedor
            {
                IdProveedor = dto.IdProveedor,
                Fecha = dto.Fecha,
                FechaVencimiento = dto.FechaVencimiento,
                TipoComprobante = dto.TipoComprobante,
                NumeroComprobante = dto.NumeroComprobante,
                Observacion = dto.Observacion,
                IdUsuario = dto.IdUsuario,
                Subtotal = subtotalTotal,
                DescuentoTotal = descuentoTotal,
                IvaTotal = ivaTotal,
                Total = subtotalTotal - descuentoTotal + ivaTotal,
                Activo = true,
                CompraProveedorDetalles = detalles
            };

            // Guardar la compra (el repo llama a SaveChangesAsync internamente)
            await _compraRepo.CreateAsync(compra);

            // Registrar ingresos de stock en el ledger
            foreach (var d in dto.Detalles)
                await _stockMovementService.RegistrarMovimientoAsync(
                    d.IdProducto,
                    TipoMovimientoStockEnum.IngresoCompra,
                    d.Cantidad,
                    $"COMPRA:{compra.IdCompraProveedor}",
                    dto.IdUsuario);

            await transaction.CommitAsync();

            var compraCreada = await _compraRepo.GetByIdAsync(compra.IdCompraProveedor);
            var responseDTO = _mapper.Map<CompraProveedorResponseDTO>(compraCreada);
            return Result<CompraProveedorResponseDTO>.Success(responseDTO);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError("Error inesperado al crear compra: {Ex}", ex);
            return Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    // ── GET ALL ───────────────────────────────────────────────────────────────

    public async Task<Result<List<CompraProveedorResponseDTO>>> GetAll()
    {
        try
        {
            var compras = await _compraRepo.GetAllAsync();
            var dtos = _mapper.Map<List<CompraProveedorResponseDTO>>(compras);
            return Result<List<CompraProveedorResponseDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al obtener compras: {Ex}", ex);
            return Result<List<CompraProveedorResponseDTO>>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    public async Task<Result<List<CompraProveedorDetailResponseDTO>>> GetAllWithDetails()
    {
        try
        {
            var compras = await _compraRepo.GetAllWithDetailsAsync();
            var dtos = _mapper.Map<List<CompraProveedorDetailResponseDTO>>(compras);
            return Result<List<CompraProveedorDetailResponseDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al obtener compras con detalles: {Ex}", ex);
            return Result<List<CompraProveedorDetailResponseDTO>>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    public async Task<Result<CompraProveedorDetailResponseDTO>> GetById(int idCompraProveedor)
    {
        try
        {
            var compra = await _compraRepo.GetByIdWithDetailsAsync(idCompraProveedor);
            if (compra == null)
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

    public async Task<Result<List<CompraProveedorDetailResponseDTO>>> GetByProveedor(int idProveedor)
    {
        try
        {
            var compras = await _compraRepo.GetByProveedorAsync(idProveedor);
            var dtos = _mapper.Map<List<CompraProveedorDetailResponseDTO>>(compras);
            return Result<List<CompraProveedorDetailResponseDTO>>.Success(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError("Error inesperado al obtener compras por proveedor: {Ex}", ex);
            return Result<List<CompraProveedorDetailResponseDTO>>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    // ── ANULAR (soft delete con motivo obligatorio + revertir stock) ──────────

    public async Task<Result<bool>> Anular(int idCompraProveedor, AnulacionCompraDTO dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            await SetAuditContextAsync();

            var compra = await _context.ComprasProveedor
                .Include(c => c.CompraProveedorDetalles)
                .FirstOrDefaultAsync(c => c.IdCompraProveedor == idCompraProveedor);

            if (compra == null)
                return Result<bool>.Failure(CompraProveedorErrorCode.compra_not_found);

            if (!compra.Activo)
                return Result<bool>.Failure(CompraProveedorErrorCode.compra_ya_inactiva);

            // Revertir stock de cada línea
            foreach (var det in compra.CompraProveedorDetalles)
                await _stockMovementService.RegistrarMovimientoAsync(
                    det.IdProducto,
                    TipoMovimientoStockEnum.EgresoAnulacionCompra,
                    -det.Cantidad,
                    $"ANULACION:COMPRA:{idCompraProveedor}",
                    _userContext.UserId);

            // Registrar motivo en Observacion
            compra.Observacion = string.IsNullOrWhiteSpace(compra.Observacion)
                ? $"ANULADO: {dto.Motivo}"
                : $"{compra.Observacion} - ANULADO: {dto.Motivo}";

            compra.Activo = false;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError("Error inesperado al anular compra: {Ex}", ex);
            return Result<bool>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    // ── EXPORT EXCEL ──────────────────────────────────────────────────────────

    public async Task<Result<byte[]>> ExportarExcelAsync()
    {
        try
        {
            var compras = await _compraRepo.GetAllWithDetailsAsync();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            using var package = new ExcelPackage();

            // ── Hoja 1: Compras ────────────────────────────────────────────
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
                wsCompras.Cells[row, 11].Value = c.Activo ? "Sí" : "No";
            }
            wsCompras.Cells[wsCompras.Dimension.Address].AutoFitColumns();

            // ── Hoja 2: Detalles ───────────────────────────────────────────
            var wsDetalles = package.Workbook.Worksheets.Add("Detalles");
            var headersDetalles = new[]
            {
                "ID Compra", "Nro. Comprobante", "Producto", "Cantidad",
                "Precio Unitario", "Descuento %", "IVA %", "Subtotal", "Total"
            };
            EstiloEncabezado(wsDetalles, headersDetalles);

            int detalleRow = 2;
            foreach (var c in compras)
            {
                foreach (var d in c.CompraProveedorDetalles)
                {
                    wsDetalles.Cells[detalleRow, 1].Value = c.IdCompraProveedor;
                    wsDetalles.Cells[detalleRow, 2].Value = c.NumeroComprobante;
                    wsDetalles.Cells[detalleRow, 3].Value = d.IdProductoNavigation?.Nombre;
                    wsDetalles.Cells[detalleRow, 4].Value = d.Cantidad;
                    wsDetalles.Cells[detalleRow, 5].Value = d.PrecioUnitario;
                    wsDetalles.Cells[detalleRow, 6].Value = d.DescuentoPorcentaje;
                    wsDetalles.Cells[detalleRow, 7].Value = d.IvaPorcentaje;
                    wsDetalles.Cells[detalleRow, 8].Value = d.Subtotal;
                    wsDetalles.Cells[detalleRow, 9].Value = d.Total;
                    detalleRow++;
                }
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

    private static void EstiloEncabezado(ExcelWorksheet ws, string[] headers)
    {
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cells[1, i + 1].Value = headers[i];
            ws.Cells[1, i + 1].Style.Font.Bold = true;
            ws.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
            ws.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(68, 114, 196));
            ws.Cells[1, i + 1].Style.Font.Color.SetColor(System.Drawing.Color.White);
        }
    }
}
