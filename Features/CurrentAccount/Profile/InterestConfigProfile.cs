using proyecto_venta_stock.Models;
using venta_stock_webapi.CurrentAccount.DTO.InterestConfigDTO;

namespace venta_stock_webapi.CurrentAccount.Profile;

public class InterestConfigProfile : AutoMapper.Profile
{
    public InterestConfigProfile()
    {
        CreateMap<CreateInterestConfigDTO, ConfiguracionInteres>()
            .ForMember(dest => dest.IdConfig, opt => opt.Ignore())
            .ForMember(dest => dest.EsActual, opt => opt.MapFrom(_ => false));

        CreateMap<UpdateInterestConfigDTO, ConfiguracionInteres>()
            .ForMember(dest => dest.EsActual, opt => opt.Ignore());

        CreateMap<ConfiguracionInteres, InterestConfigDTO>();
    }
}
