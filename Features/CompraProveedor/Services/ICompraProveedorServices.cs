using proyecto_venta_stock.CompraProveedor.DTO;
using proyecto_venta_stock.Shared.ResultPattern;

namespace proyecto_venta_stock.CompraProveedor.Services;

public interface ICompraProveedorServices
{
    Task<Result<CompraProveedorResponseDTO>> Create(CompraProveedorCreateDTO dto);
    Task<Result<CompraProveedorResponseDTO>> Update(CompraProveedorUpdateDTO dto);
    Task<Result<List<CompraProveedorResponseDTO>>> GetAll();
    Task<Result<List<CompraProveedorDetailResponseDTO>>> GetAllWithDetails();
    Task<Result<CompraProveedorDetailResponseDTO>> GetById(int idCompraProveedor);
    Task<Result<List<CompraProveedorDetailResponseDTO>>> GetByProveedor(int idProveedor);
    Task<Result<bool>> Delete(int idCompraProveedor);
    Task<Result<bool>> ToggleEstado(int idCompraProveedor);
}
