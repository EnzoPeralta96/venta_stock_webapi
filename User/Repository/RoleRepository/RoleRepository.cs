using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.User.Repository.RolRepository;

namespace proyecto_venta_stock.User.Repository.RoleRepository
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly VentaStockContext _dbContext;

        public PermissionRepository(VentaStockContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<bool> AssingPermision(int userId, List<int> roles)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> Exists(List<int> roles)
        {
            var roles_encontrados = await _dbContext.Permisos
                        .Where(p => roles.Contains(p.IdPermiso))
                        .Select(p => p.IdPermiso)
                        .ToListAsync();

            return roles.All(id => roles_encontrados.Contains(id));
            
        }
    }
}