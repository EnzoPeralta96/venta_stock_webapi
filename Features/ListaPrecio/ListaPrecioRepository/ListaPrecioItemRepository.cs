using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.ListaPrecio.ListaPrecioRepository;

public class ListaPrecioItemRepository : IListaPrecioItemRepository
{
    private readonly VentaStockContext _db;

    public ListaPrecioItemRepository(VentaStockContext db)
    {
        _db = db;
    }

    public Task<bool> ListaExistsAsync(int idLista)
        => _db.ListaPrecios.AnyAsync(l => l.IdLista == idLista);

    public Task<bool> ProductoExistsAsync(int idProducto)
        => _db.Productos.AnyAsync(p => p.IdProducto == idProducto);

    public Task<bool> ItemExistsAsync(int idLista, int idProducto)
        => _db.ProductoListaprecioProveedors
            .AnyAsync(x => x.IdLista == idLista && x.IdProducto == idProducto);

    public Task<List<ProductoListaprecioProveedor>> GetItemsByListaAsync(int idLista)
        => _db.ProductoListaprecioProveedors
            .AsNoTracking()
            .Where(x => x.IdLista == idLista)
            .Include(x => x.IdProductoNavigation)
            .OrderBy(x => x.IdProductoNavigation.Nombre)
            .ToListAsync();

    public Task<ProductoListaprecioProveedor?> GetItemAsync(int idLista, int idProducto)
        => _db.ProductoListaprecioProveedors
            .Include(x => x.IdProductoNavigation)
            .FirstOrDefaultAsync(x => x.IdLista == idLista && x.IdProducto == idProducto);

    public Task<int?> GetProductoIdByCodigoBarraAsync(string codigoBarra)
        => _db.CodigoBarras
            .Where(cb => cb.Codigo == codigoBarra && cb.Activo == true && cb.IdProducto != null)
            .Select(cb => cb.IdProducto)
            .FirstOrDefaultAsync();

    public async Task CreateAsync(ProductoListaprecioProveedor entity)
    {
        await _db.ProductoListaprecioProveedors.AddAsync(entity);
        await _db.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProductoListaprecioProveedor entity)
    {
        _db.ProductoListaprecioProveedors.Update(entity);
        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(ProductoListaprecioProveedor entity)
    {
        _db.ProductoListaprecioProveedors.Remove(entity);
        await _db.SaveChangesAsync();
    }
}
