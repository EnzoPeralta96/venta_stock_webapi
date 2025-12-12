using proyecto_venta_stock.Models;
using venta_stock_webapi.Client.DTO;

namespace venta_stock_webapi.Client.Profile
{
    public class ClientProfile : AutoMapper.Profile
    {
        public ClientProfile()
        {
            // Cliente -> ClienteResponseDTO
            CreateMap<Cliente, ClientResponseDTO>()
                .ForMember(dest => dest.TieneCuentaCorriente,
                    opt => opt.MapFrom(src => src.MovimientoCcs.Any()))
                .ForMember(dest => dest.EsEmpresa,
                    opt => opt.MapFrom(src => !string.IsNullOrEmpty(src.RazonSocial) && !string.IsNullOrEmpty(src.Cuit)));
            
            // ClienteCreateDTO -> Cliente
            CreateMap<ClientCreateDTO, Cliente>();

            // ClienteUpdateDTO -> Cliente
            CreateMap<ClientUpdateDTO, Cliente>();
        }
    }
}
