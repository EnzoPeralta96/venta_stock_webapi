using proyecto_venta_stock.Proveedor.DTO;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Shared.Paged;

namespace proyecto_venta_stock.Proveedor.Services
{
    public interface IProveedorServices
    {
        Task<Result<bool>> Create(ProveedorDTO dto);
        Task<Result<bool>> Update(ProveedorDTO dto);
        Task<Result<List<ProveedorDTO>>> GetAll();
        Task<Result<ProveedorDTO>> GetById(int idProveedor);
        Task<Result<bool>> Delete(int idProveedor);
        Task<Result<bool>> ToggleEstado(int idProveedor);
        Task<Result<PagedList<ProveedorDTO>>> ProveedoresPagedAsync(int pageIndex, int pageSize, string searchTerm, string estado = "activos");
        Task<Result<byte[]>> ExportarExcelAsync();
    }
}