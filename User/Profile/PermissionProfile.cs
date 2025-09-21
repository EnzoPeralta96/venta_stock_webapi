using proyecto_venta_stock.Models;
using proyecto_venta_stock.User.DTO;
namespace venta_stock_webapi.User.Profile;

public class PermissionProfile : AutoMapper.Profile
{
    public PermissionProfile()
    {
        CreateMap<Permiso, PermissionDTO>()
            .ForMember(dest => dest.Permiso, opt => opt.MapFrom(src => src.Permiso1));
    }
}

public class PermissionsCategoryProfile : AutoMapper.Profile
{
    public PermissionsCategoryProfile()
    {
        CreateMap<CategoriaPermiso, PermissionsCategoryDTO>()
            .ForMember(dest => dest.Permissions, opt => opt.MapFrom(src => src.Permisos));
    }
}


