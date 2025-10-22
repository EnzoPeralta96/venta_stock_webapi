using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.User.Repository.PermitRepository
{
    public interface IPermissionRepository
    {
        Task<bool> ExistsAsync(List<int> roles);
        Task AssingPermisionAsync(List<PermisoUsuario> permissionsUser);// assign permission
        Task<List<CategoriaPermiso>> GetPermissionsAsync(int? id_category_permission);
        Task<List<int>> GetPermissionsUserAsync(int userId);
        Task RemovePermissionsAsync(int userId, List<int> toRemove);
    }
}