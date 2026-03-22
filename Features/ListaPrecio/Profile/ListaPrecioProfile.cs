using AutoMapper;
using proyecto_venta_stock.ListaPrecio.DTO;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.ListaPrecio.Profile;

public class ListaPrecioProfile : AutoMapper.Profile
{
    public ListaPrecioProfile()
    {
        CreateMap<Models.ListaPrecio, ListaPrecioDTO>()
            .ForMember(dest => dest.CantidadItems,
                opt => opt.MapFrom(src => src.ProductoListaprecioProveedors.Count));

        CreateMap<ListaPrecioCreateDTO, Models.ListaPrecio>()
            .ForMember(dest => dest.Activo, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.FechaCreacion, opt => opt.MapFrom(_ => DateTime.Now));

        CreateMap<ListaPrecioUpdateDTO, Models.ListaPrecio>()
            .ForMember(dest => dest.IdProveedor, opt => opt.Ignore())
            .ForMember(dest => dest.FechaCreacion, opt => opt.Ignore())
            .ForMember(dest => dest.Activo, opt => opt.Ignore())
            .ForMember(dest => dest.IdUsuarioRegistra, opt => opt.Ignore());
    }
}
