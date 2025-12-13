using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.User.Repository.PermitRepository
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly VentaStockContext _dbContext;

        public PermissionRepository(VentaStockContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AssingPermisionAsync(List<PermisoUsuario> permissionsUser)
        {
            _dbContext.PermisoUsuarios.AddRange(permissionsUser);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> ExistsAsync(List<int> permissions)
        {
            var permissions_found = await _dbContext.Permisos
                        .Where(p => permissions.Contains(p.IdPermiso))
                        .Select(p => p.IdPermiso)
                        .ToListAsync();

            return permissions.All(id => permissions_found.Contains(id));
        }
        
        public Task<List<CategoriaPermiso>> GetPermissionsAsync(int? id_category_permission)
        {
            var query = _dbContext.CategoriaPermisos
                .Include(c => c.Permisos)
                .AsQueryable();

            if (id_category_permission.HasValue)
            {
                query = query.Where(c => c.IdCategoriaPermiso == id_category_permission.Value);
            }

            return query.ToListAsync();
        }

        public Task<List<int>> GetPermissionsUserAsync(int id)
        {
            return _dbContext.PermisoUsuarios
                .AsNoTracking()
                .Where(u => u.IdUsuario == id)
                .Select(per => per.IdPermiso)
                .ToListAsync();
        }

        public async Task RemovePermissionsAsync(int userId, List<int> toRemove)
        {
            var permissionsRemove = await _dbContext.PermisoUsuarios
                .Where(p => p.IdUsuario == userId && toRemove.Contains(p.IdPermiso))
                .ToListAsync();

            _dbContext.PermisoUsuarios.RemoveRange(permissionsRemove);

            await _dbContext.SaveChangesAsync(); 
        }
    }
}