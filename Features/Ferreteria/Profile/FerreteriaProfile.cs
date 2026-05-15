using AutoMapper;
using proyecto_venta_stock.Models;
using venta_stock_webapi.Features.Ferreteria.DTO;

namespace venta_stock_webapi.Features.Ferreteria.Profile
{
    public class FerreteriaProfile : AutoMapper.Profile
    {
        public FerreteriaProfile()
        {
            CreateMap<proyecto_venta_stock.Models.Ferreteria, FerreteriaResponseDTO>();
            CreateMap<FerreteriaUpdateDTO, proyecto_venta_stock.Models.Ferreteria>();
        }
    }
}
