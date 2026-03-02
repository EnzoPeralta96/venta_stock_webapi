using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Proveedor.ProveedorRepository
{
    public interface IProveedorRepository
    {
        Task Create(Models.Proveedor proveedor);
        Task Update(Models.Proveedor proveedor);

        Task<Models.Proveedor?> GetById(int idProveedor);
        Task<List<Models.Proveedor>> GetAll();

        Task<bool> Exists(string nombre, int? excludeId = null);

        Task Delete(Models.Proveedor proveedor);
    }
}