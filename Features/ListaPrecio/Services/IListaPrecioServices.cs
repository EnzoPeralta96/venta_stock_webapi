using proyecto_venta_stock.ListaPrecio.DTO;
using proyecto_venta_stock.Shared.ResultPattern;

namespace proyecto_venta_stock.ListaPrecio.Services;

public interface IListaPrecioServices
{
    Task<Result<List<ListaPrecioDTO>>> GetByProveedorAsync(int idProveedor);
    Task<Result<ListaPrecioDTO>> GetByIdAsync(int idLista);
    Task<Result<bool>> CreateAsync(ListaPrecioCreateDTO dto, int idUsuario);
    Task<Result<bool>> UpdateAsync(ListaPrecioUpdateDTO dto);
    Task<Result<bool>> DeleteAsync(int idLista);
    Task<Result<bool>> ToggleActivoAsync(int idLista);
}
