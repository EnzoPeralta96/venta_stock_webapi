using AutoMapper;
using proyecto_venta_stock.Models;
using venta_stock_webapi.Sale.DTO.CreditNoteReasonDTO;

namespace venta_stock_webapi.Sale.Profile;

public class CreditNoteReasonProfile : AutoMapper.Profile
{
    public CreditNoteReasonProfile()
    {
        CreateMap<CreateCreditNoteReasonDTO, MotivoNotaCredito>()
            .ForMember(dest => dest.IdMotivo, opt => opt.Ignore())
            .ForMember(dest => dest.Activo, opt => opt.MapFrom(_ => true));

        CreateMap<UpdateCreditNoteReasonDTO, MotivoNotaCredito>()
            .ForMember(dest => dest.Activo, opt => opt.Ignore());

        CreateMap<MotivoNotaCredito, CreditNoteReasonDTO>();
    }
}
