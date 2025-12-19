using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using proyecto_venta_stock.Models;
using venta_stock_webapi.Features.Audit.DTO;

namespace venta_stock_webapi.Features.Audit.Profile
{
    public class AuditProfile : AutoMapper.Profile
    {
        public AuditProfile()
        {
            CreateMap<Auditoria, AuditItemDTO>()
                .ForMember(dest => dest.ValoresAnteriores,
                    opt => opt.MapFrom(src => src.ValoresAnteriores != null
                        ? src.ValoresAnteriores.RootElement.GetRawText()
                        : null))
                .ForMember(dest => dest.ValoresNuevos,
                    opt => opt.MapFrom(src => src.ValoresNuevos != null
                        ? src.ValoresNuevos.RootElement.GetRawText()
                        : null));
        }
    }
}