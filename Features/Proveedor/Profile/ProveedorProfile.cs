using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Proveedor.DTO;

namespace proyecto_venta_stock.Proveedor.Profile
{
    public class ProveedorProfile : AutoMapper.Profile
    {
        public ProveedorProfile()
        {
            CreateMap<ProveedorDTO, Models.Proveedor>()
                .ForMember(dest => dest.Proveedor1, opt => opt.MapFrom(src => src.Nombre))
                .ForMember(dest => dest.FechaBaja, opt => opt.Ignore());

            CreateMap<Models.Proveedor, ProveedorDTO>()
                .ForMember(dest => dest.Nombre, opt => opt.MapFrom(src => src.Proveedor1));
        }
    }
}