using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Client.DTO;
using venta_stock_webapi.Shared.Paged;

namespace venta_stock_webapi.Client.Services
{
    public interface IClientService
    {
        Task<Result<ClientResponseDTO>> CreateClienteAsync(ClientCreateDTO clienteDTO);
        Task<Result<ClientResponseDTO>> GetClient(int id);
        Task<Result<PagedList<ClientResponseDTO>>> Search(int pageIndex, int pageSize, string searchTerm, string estado = "activos");
        Task<Result<ClientResponseDTO>> UpdateClient(ClientUpdateDTO clienteDTO);
        Task<Result<string>> ToggleStatus(ClientToggleStatusDTO dto);
    }
}