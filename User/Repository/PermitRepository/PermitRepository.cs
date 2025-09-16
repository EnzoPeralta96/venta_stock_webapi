using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.User.Repository.PermitRepository
{
    public class PermitRepository : IPermitRepository
    {
        private readonly VentaStockContext _dbContext;

        public PermitRepository(VentaStockContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AssingPermision(List<PermisoUsuario> permissionsUser)
        {
            _dbContext.PermisoUsuarios.AddRange(permissionsUser);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> Exists(List<int> permissions)
        {
            var permissions_found = await _dbContext.Permisos
                        .Where(p => permissions.Contains(p.IdPermiso))
                        .Select(p => p.IdPermiso)
                        .ToListAsync();

            return permissions.All(id => permissions_found.Contains(id));   
        }
        public Task<List<Permiso>> GetPermissions()
        {
            return _dbContext.Permisos.ToListAsync();
        }
    }
}