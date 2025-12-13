using proyecto_venta_stock.Models;
using proyecto_venta_stock.User.DTO;
namespace proyecto_venta_stock.User.Profile
{
    public class UserProfile : AutoMapper.Profile
    {
        public UserProfile()
        {
            CreateMap<UserCreateDTO, Usuario>()
                .ForMember(dest => dest.Usuario1, opt => opt.MapFrom(src => src.Usuario));
            
            CreateMap<UserUpdateDTO, Usuario>()
                .ForMember(dest => dest.Usuario1, opt => opt.MapFrom(src => src.Usuario));

            CreateMap<Usuario, UserDTO>()
                .ForMember(dest => dest.Usuario, opt => opt.MapFrom(src => src.Usuario1));
            
        }
    }
}