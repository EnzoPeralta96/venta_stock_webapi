using proyecto_venta_stock.Models;
using venta_stock_webapi.CurrentAccount.DTO.DebitNoteReasonDTO;

namespace venta_stock_webapi.CurrentAccount.Profile;

public class DebitNoteReasonProfile : AutoMapper.Profile
{
    public DebitNoteReasonProfile()
    {
        CreateMap<CreateDebitNoteReasonDTO, MotivoNotaDebito>()
            .ForMember(dest => dest.IdMotivo, opt => opt.Ignore())
            .ForMember(dest => dest.Activo, opt => opt.MapFrom(src => true));

        CreateMap<UpdateDebitNoteReasonDTO, MotivoNotaDebito>()
            .ForMember(dest => dest.Activo, opt => opt.Ignore());

        CreateMap<MotivoNotaDebito, DebitNoteReasonDTO>();
    }
}
