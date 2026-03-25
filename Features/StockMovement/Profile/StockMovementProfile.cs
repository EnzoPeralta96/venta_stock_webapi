using proyecto_venta_stock.Models;
using venta_stock_webapi.Features.StockMovement.DTO;

namespace venta_stock_webapi.Features.StockMovement.Profile;

public class StockMovementProfile : AutoMapper.Profile
{
    public StockMovementProfile()
    {
        CreateMap<TipoMovimientoStock, TipoMovimientoStockDTO>();
        CreateMap<CreateTipoMovimientoDTO, TipoMovimientoStock>()
            .ForMember(dest => dest.Descripcion, opt => opt.MapFrom(src => src.Descripcion ?? string.Empty))
            .ForMember(dest => dest.Activo, opt => opt.MapFrom(_ => true))
            .ForMember(dest => dest.EsSistema, opt => opt.MapFrom(_ => false));
        CreateMap<UpdateTipoMovimientoDTO, TipoMovimientoStock>()
            .ForMember(dest => dest.Descripcion, opt => opt.MapFrom(src => src.Descripcion ?? string.Empty));

        CreateMap<MovimientoStock, MovimientoStockDTO>()
            .ForMember(dest => dest.TipoMovimiento,
                opt => opt.MapFrom(src => src.IdTipoMovimientoStockNavigation.Nombre))
            .ForMember(dest => dest.Usuario,
                opt => opt.MapFrom(src => src.IdUsuarioNavigation != null
                    ? src.IdUsuarioNavigation.Nombre
                    : null));
    }
}
