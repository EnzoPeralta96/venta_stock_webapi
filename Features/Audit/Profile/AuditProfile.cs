using proyecto_venta_stock.Models;
using venta_stock_webapi.Features.Audit.DTO;

namespace venta_stock_webapi.Features.Audit.Profile
{
    public class AuditProfile : AutoMapper.Profile
    {
        public AuditProfile()
        {
            CreateMap<Auditoria, AuditItemDTO>()
                .ForMember(d => d.ValoresAnteriores, o => o.MapFrom(s => s.ValoresAnteriores))
                .ForMember(d => d.ValoresNuevos, o => o.MapFrom(s => s.ValoresNuevos));
        }
    }
}