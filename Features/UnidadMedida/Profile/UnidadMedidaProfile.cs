using AutoMapper;
using venta_stock_webapi.Features.UnidadMedida.DTO;

namespace venta_stock_webapi.Features.UnidadMedida.Profile;

public class UnidadMedidaProfile : AutoMapper.Profile
{
    public UnidadMedidaProfile()
    {
        CreateMap<proyecto_venta_stock.Models.UnidadMedida, UnidadMedidaDTO>();
    }
}
