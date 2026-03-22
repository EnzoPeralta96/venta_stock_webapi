using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.ListaPrecio.ListaPrecioRepository;

public interface IListaPrecioItemRepository
{
    Task<bool> ListaExistsAsync(int idLista);
    Task<bool> ProductoExistsAsync(int idProducto);
    Task<bool> ItemExistsAsync(int idLista, int idProducto);
    Task<List<ProductoListaprecioProveedor>> GetItemsByListaAsync(int idLista);
    Task<ProductoListaprecioProveedor?> GetItemAsync(int idLista, int idProducto);
    Task CreateAsync(ProductoListaprecioProveedor entity);
    Task UpdateAsync(ProductoListaprecioProveedor entity);
    Task DeleteAsync(ProductoListaprecioProveedor entity);
}
