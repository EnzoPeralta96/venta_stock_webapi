using proyecto_venta_stock.Proveedor.DTO;
using ProveedorModel = proyecto_venta_stock.Models.Proveedor;

namespace proyecto_venta_stock.Proveedor.Profile
{
    public class ProveedorProfile : AutoMapper.Profile
    {
        public ProveedorProfile()
        {

            CreateMap<CreateProveedorDTO, ProveedorModel>()
                .ForMember(dest => dest.Proveedor1, opt => opt.MapFrom(src => src.Nombre))
                .ForMember(dest => dest.Activo, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.FechaBaja, opt => opt.Ignore());


            CreateMap<UpdateProveedorDTO, ProveedorModel>()
                .ForMember(dest => dest.Proveedor1, opt => opt.MapFrom(src => src.Nombre))
                .ForMember(dest => dest.FechaBaja, opt => opt.Ignore());

            CreateMap<ProveedorModel, ProveedorDTO>()
                .ForMember(dest => dest.Nombre, opt => opt.MapFrom(src => src.Proveedor1));
        }
    }
}