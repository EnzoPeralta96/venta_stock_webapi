using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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

    public Task<List<ProductoListaprecioProveedor>> GetItemsWithBarcodesAsync(int idLista)
        => _db.ProductoListaprecioProveedors
            .AsNoTracking()
            .Where(x => x.IdLista == idLista)
            .Include(x => x.IdProductoNavigation)
                .ThenInclude(p => p.CodigoBarras)
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

    public async Task UpsertItemNoSaveAsync(int idLista, int idProducto, decimal precio, decimal? margen)
    {
        var existing = await _db.ProductoListaprecioProveedors
            .FirstOrDefaultAsync(x => x.IdLista == idLista && x.IdProducto == idProducto);

        if (existing != null)
        {
            existing.Precio = precio;
            existing.Margen = margen;
        }
        else
        {
            await _db.ProductoListaprecioProveedors.AddAsync(new ProductoListaprecioProveedor
            {
                IdLista = idLista,
                IdProducto = idProducto,
                Precio = precio,
                Margen = margen
            });
        }
    }

    public async Task UpdateProductoPrecioNoSaveAsync(int idProducto, decimal nuevoPrecio)
    {
        var producto = await _db.Productos.FindAsync(idProducto);
        if (producto != null)
            producto.Precio = nuevoPrecio;
    }

    public async Task UpdateProductoCostoNoSaveAsync(int idProducto, decimal costo, decimal porcentajeGanancia, decimal precio)
    {
        var producto = await _db.Productos.FindAsync(idProducto);
        if (producto == null) return;
        producto.Costo = costo;
        producto.PorcentajeGanancia = porcentajeGanancia;
        producto.Precio = precio;
    }

    public async Task UpdateProductoCostoConMargenExistenteNoSaveAsync(int idProducto, decimal costo)
    {
        var producto = await _db.Productos.FindAsync(idProducto);
        if (producto == null) return;
        producto.Costo = costo;
        producto.Precio = costo * (1 + producto.PorcentajeGanancia / 100m);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
        => _db.Database.BeginTransactionAsync();

    public Task SaveChangesAsync()
        => _db.SaveChangesAsync();
}
