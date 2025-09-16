using proyecto_venta_stock.Models;
using venta_stock_webapi.User.DTO;

namespace venta_stock_webapi.User.Profile;

public class PermissionProfile : AutoMapper.Profile
{
    public PermissionProfile()
    {
        CreateMap<Permiso, PermissionDTO>();
    }
}

