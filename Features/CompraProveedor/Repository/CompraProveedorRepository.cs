using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

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

    public Task UpdateAsync(Models.CompraProveedor compra)
    {
        _dbContext.ComprasProveedor.Update(compra);
        return _dbContext.SaveChangesAsync();
    }

    public async Task<List<Models.CompraProveedor>> GetAllAsync()
    {
        return await _dbContext.ComprasProveedor
            .AsNoTracking()
            .Include(c => c.IdProveedorNavigation)
            .Include(c => c.IdUsuarioNavigation)
            .Where(c => c.Activo)
            .OrderByDescending(c => c.Fecha)
            .ToListAsync();
    }

    public async Task<List<Models.CompraProveedor>> GetAllWithDetailsAsync()
    {
        return await _dbContext.ComprasProveedor
            .AsNoTracking()
            .Include(c => c.IdProveedorNavigation)
            .Include(c => c.IdUsuarioNavigation)
            .Include(c => c.CompraProveedorDetalles)
                .ThenInclude(d => d.IdProductoNavigation)
            .OrderByDescending(c => c.Fecha)
            .ToListAsync();
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
            .FirstOrDefaultAsync(c => c.IdCompraProveedor == idCompraProveedor);
    }

    public async Task<List<Models.CompraProveedor>> GetByProveedorAsync(int idProveedor)
    {
        return await _dbContext.ComprasProveedor
            .AsNoTracking()
            .Include(c => c.IdProveedorNavigation)
            .Include(c => c.IdUsuarioNavigation)
            .Include(c => c.CompraProveedorDetalles)
                .ThenInclude(d => d.IdProductoNavigation)
            .Where(c => c.IdProveedor == idProveedor && c.Activo)
            .OrderByDescending(c => c.Fecha)
            .ToListAsync();
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

    public Task ToggleEstadoAsync(int idCompraProveedor, bool activo)
    {
        return _dbContext.ComprasProveedor
            .Where(c => c.IdCompraProveedor == idCompraProveedor)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(c => c.Activo, activo));
    }
}
