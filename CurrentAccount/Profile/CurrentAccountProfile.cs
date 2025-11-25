using proyecto_venta_stock.Models;
using venta_stock_webapi.CurrentAccount.DTO;

namespace venta_stock_webapi.CurrentAccount.Profile;

public class CurrentAccountProfile : AutoMapper.Profile
{
    public CurrentAccountProfile()
    {
        CreateMap<CreateAccountConfigDTO, ConfiguracionCc>()
            .ForMember(dest => dest.IdConfig, opt => opt.Ignore())
            .ForMember(dest => dest.Activo, opt => opt.MapFrom(src => true));

        CreateMap<UpdateAccountConfigDTO, ConfiguracionCc>()
            .ForMember(dest => dest.Activo, opt => opt.Ignore());
            
        CreateMap<ConfiguracionCc, AccountConfigDTO>();
    }
}