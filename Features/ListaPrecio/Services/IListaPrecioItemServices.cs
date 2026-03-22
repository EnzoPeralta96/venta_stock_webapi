using proyecto_venta_stock.ListaPrecio.DTO;
using proyecto_venta_stock.Shared.ResultPattern;

namespace proyecto_venta_stock.ListaPrecio.Services;

public interface IListaPrecioItemServices
{
    Task<Result<List<ListaPrecioItemDTO>>> GetItemsAsync(int idLista);
    Task<Result<bool>> AddItemAsync(int idLista, ListaPrecioItemUpsertDTO dto);
    Task<Result<bool>> UpdateItemAsync(int idLista, int idProducto, ListaPrecioItemUpsertDTO dto);
    Task<Result<bool>> DeleteItemAsync(int idLista, int idProducto);
}
