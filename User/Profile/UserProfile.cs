namespace proyecto_venta_stock.User.Profile
{
    using AutoMapper;
    using proyecto_venta_stock.Models;
    using proyecto_venta_stock.User.DTO;

    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<UserDTO, Usuario>()
                .ForMember(dest => dest.Usuario1, opt => opt.MapFrom(src => src.Usuario));
        }
    }
}