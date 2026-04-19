using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Models;
using venta_stock_webapi.CurrentAccount.DTO.MovementDTO;

namespace venta_stock_webapi.CurrentAccount.Profile
{
    public class CurrentAccountProfile : AutoMapper.Profile
    {
        public CurrentAccountProfile()
        {
            CreateMap<MovimientoCc, AccountMovementDTO>()
                .ForMember(dest => dest.TipoMovimiento, opt => opt.MapFrom(src => src.IdTipoMovimientoNavigation.Nombre))
                .ForMember(dest => dest.Detalle, opt => opt.MapFrom(src => !string.IsNullOrWhiteSpace(src.Detalle) ? src.Detalle : src.IdTipoMovimientoNavigation.Accion))
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.IdEstadoNavigation.Estado1))
                .ForMember(dest => dest.UsuarioRegistra, opt => opt.MapFrom(src => src.IdUsuarioRegistraNavigation.Nombre + " " + src.IdUsuarioRegistraNavigation.Apellido))
                .ForMember(dest => dest.CodigoVenta, opt => opt.MapFrom(src => src.IdVentaNavigation != null ? src.IdVentaNavigation.CodigoVenta : null))
                .ForMember(dest => dest.FechaVenta, opt => opt.MapFrom(src => src.IdVentaNavigation != null ? src.IdVentaNavigation.Fecha : null))
                .ForMember(dest => dest.TotalVenta, opt => opt.MapFrom(src => src.IdVentaNavigation != null ? src.IdVentaNavigation.Total : null))
                .ForMember(dest => dest.EstadoVenta, opt => opt.MapFrom(src => src.IdVentaNavigation != null && src.IdVentaNavigation.IdEstadoNavigation != null ? src.IdVentaNavigation.IdEstadoNavigation.Estado1 : null))
                .ForMember(dest => dest.MontoPagado, opt => opt.MapFrom(src => src.MontoPagado))
                .ForMember(dest => dest.EstadoPago, opt => opt.MapFrom(src =>
                    src.IdTipoMovimiento != 5 ? null :
                    src.IdVentaNavigation != null && src.IdVentaNavigation.IdEstado == 5 ? "anulado" :
                    (src.MontoPagado == null || src.MontoPagado == 0) ? "pendiente" :
                    src.MontoPagado >= src.Importe ? "pagado" : "parcial"))
                .ForMember(dest => dest.EsAnulado, opt => opt.MapFrom(src => src.EsAnulado))
                .ForMember(dest => dest.IdMotivoNd, opt => opt.MapFrom(src => src.IdMotivoNd))
                .ForMember(dest => dest.MotivoNd, opt => opt.MapFrom(src =>
                    src.IdMotivoNdNavigation != null ? src.IdMotivoNdNavigation.Nombre : null))
                .ForMember(dest => dest.IdMotivoNc, opt => opt.MapFrom(src => src.IdMotivoNc))
                .ForMember(dest => dest.MotivoNc, opt => opt.MapFrom(src =>
                    src.IdMotivoNcNavigation != null ? src.IdMotivoNcNavigation.Nombre : null));

            CreateMap<CreateCurrentAccountDTO, MovimientoCc>()
                .ForMember(dest => dest.Importe, opt => opt.MapFrom(src => 0))
                .ForMember(dest => dest.Fecha, opt => opt.MapFrom(src => DateTime.Now))
                //Estado : aprobado = 2
                .ForMember(dest => dest.IdEstado, opt => opt.MapFrom(src => 2))
                .ForMember(dest => dest.IdTipoMovimiento, opt => opt.MapFrom(src => 2));

            CreateMap<TipoMovimiento, TypeMovementDTO>()
                .ForMember(dest => dest.IdTipoMovimiento, opt => opt.MapFrom(src => src.IdMovimiento));
        }
    }
}