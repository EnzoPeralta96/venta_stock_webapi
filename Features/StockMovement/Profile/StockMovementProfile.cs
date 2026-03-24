using proyecto_venta_stock.Models;
using venta_stock_webapi.Features.StockMovement.DTO;

namespace venta_stock_webapi.Features.StockMovement.Profile;

public class StockMovementProfile : AutoMapper.Profile
{
    public StockMovementProfile()
    {
        CreateMap<TipoMovimientoStock, TipoMovimientoStockDTO>();

        CreateMap<MovimientoStock, MovimientoStockDTO>()
            .ForMember(dest => dest.TipoMovimiento,
                opt => opt.MapFrom(src => src.IdTipoMovimientoStockNavigation.Nombre))
            .ForMember(dest => dest.Usuario,
                opt => opt.MapFrom(src => src.IdUsuarioNavigation != null
                    ? src.IdUsuarioNavigation.Nombre
                    : null));
    }
}
