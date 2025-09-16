using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.User.Repository.PermitRepository
{
    public interface IPermissionRepository
    {
        Task<bool> Exists(List<int> roles);
        Task AssingPermision(List<PermisoUsuario> permissionsUser);// assign permission
        Task<List<Permiso>> GetPermissions(int id_category_permission);
    }
}