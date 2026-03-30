using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Proveedor.ProveedorRepository
{
    public interface IProveedorRepository
    {
        void Create(Models.Proveedor proveedor);
        void Update(Models.Proveedor proveedor);
        Task<Models.Proveedor?> GetById(int idProveedor);
        Task<List<Models.Proveedor>> GetAll();
        IQueryable<Models.Proveedor> ProveedoresQueryable(string searchTerm);
        Task<bool> Exists(string nombre, int? excludeId = null);
        Task<bool> Exists(int idProveedor);
        void Delete(Models.Proveedor proveedor);
        Task SaveChangesAsync();

    }
}