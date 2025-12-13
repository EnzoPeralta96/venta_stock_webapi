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
                .ForMember(dest => dest.Detalle, opt => opt.MapFrom(src => src.IdTipoMovimientoNavigation.Accion))
                .ForMember(dest => dest.Estado, opt => opt.MapFrom(src => src.IdEstadoNavigation.Estado1))
                .ForMember(dest => dest.UsuarioRegistra, opt => opt.MapFrom(src => src.IdUsuarioRegistraNavigation.Nombre + " " + src.IdUsuarioRegistraNavigation.Apellido))
                .ForMember(dest => dest.UsuarioAutoriza, opt => opt.MapFrom(src => src.IdUsuarioAutorizaNavigation != null ? src.IdUsuarioAutorizaNavigation.Nombre + " " + src.IdUsuarioAutorizaNavigation.Apellido : "No requiere autorización"));

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