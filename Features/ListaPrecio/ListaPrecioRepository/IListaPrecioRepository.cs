using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.ListaPrecio.ListaPrecioRepository;

public interface IListaPrecioRepository
{
    Task<Models.ListaPrecio?> GetByIdAsync(int idLista);
    Task<List<Models.ListaPrecio>> GetByProveedorAsync(int idProveedor);
    Task<bool> ExistsNameAsync(int idProveedor, string nombre, int? excludeId = null);
    Task<bool> HasItemsAsync(int idLista);
    Task CreateAsync(Models.ListaPrecio lista);
    Task UpdateAsync(Models.ListaPrecio lista);
    Task DeleteAsync(Models.ListaPrecio lista);
}
