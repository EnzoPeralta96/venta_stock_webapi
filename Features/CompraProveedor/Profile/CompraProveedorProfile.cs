using AutoMapper;
using proyecto_venta_stock.CompraProveedor.DTO;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.CompraProveedor.Profile;

public class CompraProveedorProfile : AutoMapper.Profile
{
    public CompraProveedorProfile()
    {
        // CompraProveedor -> CompraProveedorResponseDTO
        CreateMap<Models.CompraProveedor, CompraProveedorResponseDTO>()
            .ForMember(dest => dest.NombreProveedor,
                opt => opt.MapFrom(src => src.IdProveedorNavigation != null
                    ? src.IdProveedorNavigation.Proveedor1
                    : string.Empty))
            .ForMember(dest => dest.NombreUsuario,
                opt => opt.MapFrom(src => src.IdUsuarioNavigation != null
                    ? src.IdUsuarioNavigation.Nombre + " " + src.IdUsuarioNavigation.Apellido
                    : null));

        // CompraProveedor -> CompraProveedorDetailResponseDTO (con detalles)
        CreateMap<Models.CompraProveedor, CompraProveedorDetailResponseDTO>()
            .ForMember(dest => dest.NombreProveedor,
                opt => opt.MapFrom(src => src.IdProveedorNavigation != null
                    ? src.IdProveedorNavigation.Proveedor1
                    : string.Empty))
            .ForMember(dest => dest.NombreUsuario,
                opt => opt.MapFrom(src => src.IdUsuarioNavigation != null
                    ? src.IdUsuarioNavigation.Nombre + " " + src.IdUsuarioNavigation.Apellido
                    : null))
            .ForMember(dest => dest.Detalles,
                opt => opt.MapFrom(src => src.CompraProveedorDetalles));

        // CompraProveedorDetalle -> CompraProveedorDetalleResponseDTO
        CreateMap<CompraProveedorDetalle, CompraProveedorDetalleResponseDTO>()
            .ForMember(dest => dest.NombreProducto,
                opt => opt.MapFrom(src => src.IdProductoNavigation != null
                    ? src.IdProductoNavigation.Nombre
                    : string.Empty));
    }
}
