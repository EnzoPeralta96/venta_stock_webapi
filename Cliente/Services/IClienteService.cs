using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Cliente.DTO;


namespace venta_stock_webapi.Cliente.Services
{
    public interface IClienteService
    {
        Task<Result<ClienteResponseDTO>> CreateClienteAsync(ClienteCreateDTO clienteDTO);
    }
}