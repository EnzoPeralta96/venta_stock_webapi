using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.User.DTO;

namespace venta_stock_webapi.User.Services
{
    public interface IPermissionService
    {
        Task<Result<List<PermissionsCategoryDTO>>> GetPermissions(int? id_category_permission);
    }
}