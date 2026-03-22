using AutoMapper;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.CompraProveedor.DTO;
using proyecto_venta_stock.CompraProveedor.Message;
using proyecto_venta_stock.CompraProveedor.Repository;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Shared.Identity;

namespace proyecto_venta_stock.CompraProveedor.Services;

public class CompraProveedorServices : ICompraProveedorServices
{
    private readonly ILogger<CompraProveedorServices> _logger;
    private readonly IMapper _mapper;
    private readonly ICompraProveedorRepository _compraRepo;
    private readonly VentaStockContext _context;
    private readonly IUserContext _userContext;

    public CompraProveedorServices(
        ILogger<CompraProveedorServices> logger,
        IMapper mapper,
        ICompraProveedorRepository compraRepo,
        VentaStockContext context,
        IUserContext userContext)
    {
        _logger = logger;
        _mapper = mapper;
        _compraRepo = compraRepo;
        _context = context;
        _userContext = userContext;
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

    /// <summary>Suma <paramref name="cantidad"/> al stock del producto. Usa ExecuteUpdateAsync para evitar conflictos con el change tracker.</summary>
    private Task SumarStockAsync(int idProducto, int cantidad) =>
        _context.Productos
            .Where(p => p.IdProducto == idProducto)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => (p.Stock ?? 0) + cantidad));

    /// <summary>
    /// Resta <paramref name="cantidad"/> al stock del producto, con piso en 0.
    /// Usa GREATEST(0, stock - cantidad) para evitar valores negativos sin bloquear la operación.
    /// </summary>
    private Task RestarStockAsync(int idProducto, int cantidad) =>
        _context.Database.ExecuteSqlRawAsync(
            "UPDATE producto SET stock = GREATEST(0, COALESCE(stock, 0) - {0}) WHERE id_producto = {1}",
            cantidad, idProducto);

    // ── Calcular línea de detalle ──────────────────────────────────────────────

    private static (decimal subtotal, decimal descuento, decimal iva, decimal total) CalcLinea(
        int cantidad, decimal precioUnitario, decimal descuentoPorcentaje, decimal ivaPorcentaje)
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
            if (dto.IdUsuario.HasValue)
            {
                var usuarioExiste = await _context.Usuarios
                    .AnyAsync(u => u.IdUsuario == dto.IdUsuario.Value && u.FechaBaja == null);
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

            // Actualizar stock con ExecuteUpdateAsync (opera sobre la misma transacción)
            foreach (var d in dto.Detalles)
                await SumarStockAsync(d.IdProducto, d.Cantidad);

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

    // ── UPDATE ────────────────────────────────────────────────────────────────

    public async Task<Result<CompraProveedorResponseDTO>> Update(CompraProveedorUpdateDTO dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Setear contexto de auditoría antes de cualquier SQL raw
            await SetAuditContextAsync();

            if (dto.Detalles == null || dto.Detalles.Count == 0)
                return Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.sin_detalles);

            var compraExistente = await _context.ComprasProveedor
                .Include(c => c.CompraProveedorDetalles)
                .FirstOrDefaultAsync(c => c.IdCompraProveedor == dto.IdCompraProveedor && c.Activo);

            if (compraExistente == null)
                return Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.compra_not_found);

            // Validar proveedor
            var proveedorExiste = await _context.Proveedors
                .AnyAsync(p => p.IdProveedor == dto.IdProveedor && p.Activo);
            if (!proveedorExiste)
                return Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.proveedor_not_found);

            // Validar usuario
            if (dto.IdUsuario.HasValue)
            {
                var usuarioExiste = await _context.Usuarios
                    .AnyAsync(u => u.IdUsuario == dto.IdUsuario.Value && u.FechaBaja == null);
                if (!usuarioExiste)
                    return Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.usuario_not_found);
            }

            // Validar número de comprobante duplicado (excluir la compra actual)
            if (!string.IsNullOrWhiteSpace(dto.NumeroComprobante))
            {
                var duplicado = await _compraRepo.ExistsByNumeroComprobanteAsync(
                    dto.NumeroComprobante, dto.IdProveedor, dto.IdCompraProveedor);
                if (duplicado)
                    return Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.numero_comprobante_duplicado);
            }

            // Validar que los nuevos productos existan (sin filtro de Activo: al editar
            // se permiten productos que existían aunque hayan sido dados de baja)
            var idsProductoNuevos = dto.Detalles.Select(d => d.IdProducto).Distinct().ToList();
            var productosNuevosCount = await _context.Productos
                .Where(p => idsProductoNuevos.Contains(p.IdProducto))
                .AsNoTracking()
                .CountAsync();

            if (productosNuevosCount != idsProductoNuevos.Count)
                return Result<CompraProveedorResponseDTO>.Failure(CompraProveedorErrorCode.producto_not_found);

            // Revertir stock de los detalles anteriores (con GREATEST 0 para no bloquear)
            foreach (var det in compraExistente.CompraProveedorDetalles)
                await RestarStockAsync(det.IdProducto, det.Cantidad);

            // Eliminar detalles anteriores y reemplazar con los nuevos
            _context.ComprasProveedorDetalle.RemoveRange(compraExistente.CompraProveedorDetalles);

            // Calcular nuevos totales
            decimal subtotalTotal = 0, descuentoTotal = 0, ivaTotal = 0;
            var nuevosDetalles = new List<Models.CompraProveedorDetalle>();

            foreach (var d in dto.Detalles)
            {
                var (sub, desc, iva, tot) = CalcLinea(d.Cantidad, d.PrecioUnitario, d.DescuentoPorcentaje, d.IvaPorcentaje);
                subtotalTotal += sub;
                descuentoTotal += desc;
                ivaTotal += iva;

                nuevosDetalles.Add(new Models.CompraProveedorDetalle
                {
                    IdCompraProveedor = compraExistente.IdCompraProveedor,
                    IdProducto = d.IdProducto,
                    Cantidad = d.Cantidad,
                    PrecioUnitario = d.PrecioUnitario,
                    DescuentoPorcentaje = d.DescuentoPorcentaje,
                    IvaPorcentaje = d.IvaPorcentaje,
                    Subtotal = sub,
                    Total = tot
                });
            }

            // Actualizar encabezado
            compraExistente.IdProveedor = dto.IdProveedor;
            compraExistente.Fecha = dto.Fecha;
            compraExistente.FechaVencimiento = dto.FechaVencimiento;
            compraExistente.TipoComprobante = dto.TipoComprobante;
            compraExistente.NumeroComprobante = dto.NumeroComprobante;
            compraExistente.Observacion = dto.Observacion;
            compraExistente.IdUsuario = dto.IdUsuario;
            compraExistente.Subtotal = subtotalTotal;
            compraExistente.DescuentoTotal = descuentoTotal;
            compraExistente.IvaTotal = ivaTotal;
            compraExistente.Total = subtotalTotal - descuentoTotal + ivaTotal;
            compraExistente.CompraProveedorDetalles = nuevosDetalles;

            await _context.SaveChangesAsync();

            // Sumar stock de los nuevos detalles
            foreach (var d in dto.Detalles)
                await SumarStockAsync(d.IdProducto, d.Cantidad);

            await transaction.CommitAsync();

            var compraActualizada = await _compraRepo.GetByIdAsync(compraExistente.IdCompraProveedor);
            var responseDTO = _mapper.Map<CompraProveedorResponseDTO>(compraActualizada);
            return Result<CompraProveedorResponseDTO>.Success(responseDTO);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError("Error inesperado al actualizar compra: {Ex}", ex);
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

    // ── DELETE (soft delete + revertir stock) ─────────────────────────────────

    public async Task<Result<bool>> Delete(int idCompraProveedor)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Setear contexto de auditoría antes de cualquier SQL raw
            await SetAuditContextAsync();

            var compra = await _context.ComprasProveedor
                .Include(c => c.CompraProveedorDetalles)
                .FirstOrDefaultAsync(c => c.IdCompraProveedor == idCompraProveedor);

            if (compra == null)
                return Result<bool>.Failure(CompraProveedorErrorCode.compra_not_found);

            if (!compra.Activo)
                return Result<bool>.Failure(CompraProveedorErrorCode.compra_ya_inactiva);

            // Revertir stock (con GREATEST 0 para tolerar inconsistencias históricas)
            foreach (var det in compra.CompraProveedorDetalles)
                await RestarStockAsync(det.IdProducto, det.Cantidad);

            // Soft delete
            compra.Activo = false;
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError("Error inesperado al eliminar compra: {Ex}", ex);
            return Result<bool>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }

    // ── TOGGLE ESTADO (activo ↔ inactivo + stock) ──────────────────────────────

    public async Task<Result<bool>> ToggleEstado(int idCompraProveedor)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Setear contexto de auditoría antes de cualquier SQL raw
            await SetAuditContextAsync();

            var compra = await _context.ComprasProveedor
                .Include(c => c.CompraProveedorDetalles)
                .FirstOrDefaultAsync(c => c.IdCompraProveedor == idCompraProveedor);

            if (compra == null)
                return Result<bool>.Failure(CompraProveedorErrorCode.compra_not_found);

            if (compra.Activo)
            {
                // Desactivar → revertir stock (con GREATEST 0 para tolerar inconsistencias históricas)
                foreach (var det in compra.CompraProveedorDetalles)
                    await RestarStockAsync(det.IdProducto, det.Cantidad);

                compra.Activo = false;
            }
            else
            {
                // Reactivar → sumar stock nuevamente
                foreach (var det in compra.CompraProveedorDetalles)
                    await SumarStockAsync(det.IdProducto, det.Cantidad);

                compra.Activo = true;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError("Error inesperado al cambiar estado de compra: {Ex}", ex);
            return Result<bool>.Failure(CompraProveedorErrorCode.error_inesperado);
        }
    }
}
