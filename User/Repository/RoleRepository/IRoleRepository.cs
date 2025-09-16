using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace proyecto_venta_stock.User.Repository.RolRepository
{
    public interface IPermissionRepository
    {
        Task<bool> Exists(List<int> roles);
        Task<bool> AssingPermision(int userId, List<int> roles);// assign permission
    }
}