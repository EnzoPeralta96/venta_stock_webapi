using Microsoft.EntityFrameworkCore.Storage;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.ListaPrecio.ListaPrecioRepository;

public interface IListaPrecioItemRepository
{
    Task<bool> ListaExistsAsync(int idLista);
    Task<bool> ProductoExistsAsync(int idProducto);
    Task<bool> ItemExistsAsync(int idLista, int idProducto);
    Task<List<ProductoListaprecioProveedor>> GetItemsByListaAsync(int idLista);
    Task<List<ProductoListaprecioProveedor>> GetItemsWithBarcodesAsync(int idLista);
    Task<ProductoListaprecioProveedor?> GetItemAsync(int idLista, int idProducto);
    Task<int?> GetProductoIdByCodigoBarraAsync(string codigoBarra);
    Task CreateAsync(ProductoListaprecioProveedor entity);
    Task UpdateAsync(ProductoListaprecioProveedor entity);
    Task DeleteAsync(ProductoListaprecioProveedor entity);
    // Import-specific: no individual SaveChanges, caller manages transaction
    Task UpsertItemNoSaveAsync(int idLista, int idProducto, decimal precio, decimal? margen);
    Task UpdateProductoPrecioNoSaveAsync(int idProducto, decimal nuevoPrecio);
    Task UpdateProductoCostoNoSaveAsync(int idProducto, decimal costo, decimal porcentajeGanancia, decimal precio);
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task SaveChangesAsync();
}
