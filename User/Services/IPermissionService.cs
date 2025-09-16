using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.User.DTO;

namespace venta_stock_webapi.User.Services
{
    public interface IPermissionService
    {
        Task<Result<List<PermissionDTO>>> GetPermissions(int id_category_permission);
    }
}