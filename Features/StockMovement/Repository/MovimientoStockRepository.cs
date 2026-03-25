using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Features.StockMovement.Repository;

public class MovimientoStockRepository : IMovimientoStockRepository
{
    private readonly VentaStockContext _context;

    public MovimientoStockRepository(VentaStockContext context)
    {
        _context = context;
    }

    public void Add(MovimientoStock movimientoStock)
    {
        _context.MovimientoStocks.Add(movimientoStock);
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public Task<List<TipoMovimientoStock>> GetTiposAsync()
    {
        return _context.TipoMovimientoStocks
            .AsNoTracking()
            .Where(t => t.Activo)
            .OrderBy(t => t.IdTipoMovimientoStock)
            .ToListAsync();
    }

    public Task<List<TipoMovimientoStock>> GetTiposAdminAsync()
    {
        return _context.TipoMovimientoStocks
            .AsNoTracking()
            .OrderBy(t => t.IdTipoMovimientoStock)
            .ToListAsync();
    }

    public Task<TipoMovimientoStock?> GetTipoByIdAsync(int id)
    {
        return _context.TipoMovimientoStocks
            .FirstOrDefaultAsync(t => t.IdTipoMovimientoStock == id);
    }

    public Task<bool> NombreEnUsoAsync(string nombre, int? excludeId = null)
    {
        return _context.TipoMovimientoStocks
            .AnyAsync(t => t.Nombre == nombre && (excludeId == null || t.IdTipoMovimientoStock != excludeId));
    }

    public void AddTipo(TipoMovimientoStock tipo)
    {
        _context.TipoMovimientoStocks.Add(tipo);
    }

    public IQueryable<MovimientoStock> MovementsQueryable(int idProducto, int? idTipoMovimiento)
    {
        var query = _context.MovimientoStocks
            .AsNoTracking()
            .Where(m => m.IdProducto == idProducto);

        if (idTipoMovimiento.HasValue)
            query = query.Where(m => m.IdTipoMovimientoStock == idTipoMovimiento.Value);

        return query.OrderByDescending(m => m.Fecha);
    }
}
