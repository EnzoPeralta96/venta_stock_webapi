using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using venta_stock_webapi.Shared.Paged;

namespace proyecto_venta_stock.CompraProveedor.Repository;

public class CompraProveedorRepository : ICompraProveedorRepository
{
    private readonly VentaStockContext _dbContext;

    public CompraProveedorRepository(VentaStockContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Models.CompraProveedor> CreateAsync(Models.CompraProveedor compra)
    {
        await _dbContext.ComprasProveedor.AddAsync(compra);
        await _dbContext.SaveChangesAsync();
        return compra;
    }

    // Usado solo internamente (exportación Excel/PDF)
    public async Task<List<Models.CompraProveedor>> GetAllWithDetailsAsync(DateOnly? fechaDesde, DateOnly? fechaHasta)
    {
        var query = _dbContext.ComprasProveedor
            .AsNoTracking()
            .Include(c => c.IdProveedorNavigation)
            .Include(c => c.IdUsuarioNavigation)
            .Include(c => c.CompraProveedorDetalles)
                .ThenInclude(d => d.IdProductoNavigation)
                    .ThenInclude(p => p.IdUnidadMedidaNavigation)
            .AsQueryable();

        if (fechaDesde.HasValue) query = query.Where(c => c.Fecha >= fechaDesde.Value);
        if (fechaHasta.HasValue) query = query.Where(c => c.Fecha <= fechaHasta.Value);

        return await query.OrderByDescending(c => c.Fecha).ToListAsync();
    }

    public async Task<PagedList<Models.CompraProveedor>> GetPagedWithDetailsAsync(
        int pageIndex, int pageSize, string? search, bool? activo, DateOnly? fechaDesde, DateOnly? fechaHasta)
    {
        var query = _dbContext.ComprasProveedor
            .AsNoTracking()
            .Include(c => c.IdProveedorNavigation)
            .Include(c => c.IdUsuarioNavigation)
            .Include(c => c.CompraProveedorDetalles)
                .ThenInclude(d => d.IdProductoNavigation)
            .AsQueryable();

        if (activo.HasValue)
            query = query.Where(c => c.Activo == activo.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.ToLower();
            query = query.Where(c =>
                (c.IdProveedorNavigation != null && c.IdProveedorNavigation.Proveedor1.ToLower().Contains(q)) ||
                (c.NumeroComprobante != null && c.NumeroComprobante.ToLower().Contains(q)) ||
                (c.TipoComprobante != null && c.TipoComprobante.ToLower().Contains(q)));
        }

        if (fechaDesde.HasValue) query = query.Where(c => c.Fecha >= fechaDesde.Value);
        if (fechaHasta.HasValue) query = query.Where(c => c.Fecha <= fechaHasta.Value);

        query = query.OrderByDescending(c => c.Fecha);

        return await PagedList<Models.CompraProveedor>.CreateAsync(query, pageIndex, pageSize);
    }

    public async Task<PagedList<Models.CompraProveedor>> GetPagedByProveedorAsync(
        int idProveedor, int pageIndex, int pageSize, bool? activo, DateOnly? fechaDesde, DateOnly? fechaHasta)
    {
        var query = _dbContext.ComprasProveedor
            .AsNoTracking()
            .Include(c => c.IdProveedorNavigation)
            .Include(c => c.IdUsuarioNavigation)
            .Include(c => c.CompraProveedorDetalles)
                .ThenInclude(d => d.IdProductoNavigation)
            .Where(c => c.IdProveedor == idProveedor)
            .AsQueryable();

        if (activo.HasValue)
            query = query.Where(c => c.Activo == activo.Value);

        if (fechaDesde.HasValue) query = query.Where(c => c.Fecha >= fechaDesde.Value);
        if (fechaHasta.HasValue) query = query.Where(c => c.Fecha <= fechaHasta.Value);

        query = query.OrderByDescending(c => c.Fecha);

        return await PagedList<Models.CompraProveedor>.CreateAsync(query, pageIndex, pageSize);
    }

    public async Task<Models.CompraProveedor?> GetByIdAsync(int idCompraProveedor)
    {
        return await _dbContext.ComprasProveedor
            .AsNoTracking()
            .Include(c => c.IdProveedorNavigation)
            .Include(c => c.IdUsuarioNavigation)
            .FirstOrDefaultAsync(c => c.IdCompraProveedor == idCompraProveedor);
    }

    public async Task<Models.CompraProveedor?> GetByIdWithDetailsAsync(int idCompraProveedor)
    {
        return await _dbContext.ComprasProveedor
            .AsNoTracking()
            .Include(c => c.IdProveedorNavigation)
            .Include(c => c.IdUsuarioNavigation)
            .Include(c => c.CompraProveedorDetalles)
                .ThenInclude(d => d.IdProductoNavigation)
                    .ThenInclude(p => p.IdUnidadMedidaNavigation)
            .FirstOrDefaultAsync(c => c.IdCompraProveedor == idCompraProveedor);
    }

    public async Task<List<Models.CompraProveedor>> GetAllByProveedorWithDetailsAsync(
        int idProveedor, DateOnly? fechaDesde, DateOnly? fechaHasta)
    {
        var query = _dbContext.ComprasProveedor
            .AsNoTracking()
            .Include(c => c.IdProveedorNavigation)
            .Include(c => c.IdUsuarioNavigation)
            .Include(c => c.CompraProveedorDetalles)
                .ThenInclude(d => d.IdProductoNavigation)
                    .ThenInclude(p => p.IdUnidadMedidaNavigation)
            .Where(c => c.IdProveedor == idProveedor)
            .AsQueryable();

        if (fechaDesde.HasValue) query = query.Where(c => c.Fecha >= fechaDesde.Value);
        if (fechaHasta.HasValue) query = query.Where(c => c.Fecha <= fechaHasta.Value);

        return await query.OrderByDescending(c => c.Fecha).ToListAsync();
    }

    public Task<Models.CompraProveedor?> GetByIdForUpdateAsync(int idCompraProveedor)
    {
        return _dbContext.ComprasProveedor
            .Include(c => c.CompraProveedorDetalles)
            .FirstOrDefaultAsync(c => c.IdCompraProveedor == idCompraProveedor);
    }

    public Task<bool> ExistsByNumeroComprobanteAsync(string numeroComprobante, int idProveedor, int? excludeId = null)
    {
        return _dbContext.ComprasProveedor
            .AnyAsync(c =>
                c.NumeroComprobante == numeroComprobante &&
                c.IdProveedor == idProveedor &&
                c.Activo &&
                (excludeId == null || c.IdCompraProveedor != excludeId.Value));
    }

    public Task SaveChangesAsync()
    {
       return _dbContext.SaveChangesAsync();
    }
}
