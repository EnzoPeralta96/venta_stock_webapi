using AutoMapper;
using proyecto_venta_stock.Models;
using venta_stock_webapi.Cliente.DTO;

namespace venta_stock_webapi.Cliente.Profile
{
    public class ClienteProfile : AutoMapper.Profile
    {
        public ClienteProfile()
        {
            // Cliente -> ClienteResponseDTO
            CreateMap<proyecto_venta_stock.Models.Cliente, ClienteResponseDTO>()
                .ForMember(dest => dest.TieneCuentaCorriente,
                    opt => opt.MapFrom(src => src.MovimientoCcs.Any()));

            // ClienteCreateDTO -> Cliente
            CreateMap<ClienteCreateDTO, proyecto_venta_stock.Models.Cliente>();

            // ClienteUpdateDTO -> Cliente
            CreateMap<ClienteUpdateDTO, proyecto_venta_stock.Models.Cliente>();
        }
    }
}
