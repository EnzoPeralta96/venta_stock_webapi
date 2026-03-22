using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.ListaPrecio.ListaPrecioRepository;

public class ListaPrecioRepository : IListaPrecioRepository
{
    private readonly VentaStockContext _dbContext;

    public ListaPrecioRepository(VentaStockContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Models.ListaPrecio?> GetByIdAsync(int idLista)
    {
        return _dbContext.ListaPrecios
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.IdLista == idLista);
    }

    public Task<List<Models.ListaPrecio>> GetByProveedorAsync(int idProveedor)
    {
        return _dbContext.ListaPrecios
            .AsNoTracking()
            .Include(l => l.ProductoListaprecioProveedors)
            .Where(l => l.IdProveedor == idProveedor)
            .OrderByDescending(l => l.FechaCreacion)
            .ToListAsync();
    }

    public Task<bool> HasItemsAsync(int idLista)
    {
        return _dbContext.ProductoListaprecioProveedors
            .AnyAsync(p => p.IdLista == idLista);
    }

    public Task<bool> ExistsNameAsync(int idProveedor, string nombre, int? excludeId = null)
    {
        return _dbContext.ListaPrecios
            .AnyAsync(l =>
                l.IdProveedor == idProveedor &&
                l.Nombre!.ToLower() == nombre.ToLower() &&
                (excludeId == null || l.IdLista != excludeId));
    }

    public async Task CreateAsync(Models.ListaPrecio lista)
    {
        await _dbContext.ListaPrecios.AddAsync(lista);
        await _dbContext.SaveChangesAsync();
    }

    public async Task UpdateAsync(Models.ListaPrecio lista)
    {
        _dbContext.ListaPrecios.Update(lista);
        await _dbContext.SaveChangesAsync();
    }

    public async Task DeleteAsync(Models.ListaPrecio lista)
    {
        _dbContext.ListaPrecios.Remove(lista);
        await _dbContext.SaveChangesAsync();
    }
}
