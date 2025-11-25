using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Client.Repository
{
    public interface IClientRepository
    {
        Task<bool> DniExistsAsync(string dni);
        Task<bool> CuitExistsAsync(string cuit);
        Task<bool> EmailExistsAsync(string email);
        Task<bool> EnterpriseExistsAsync(string enterprise);
        Task<bool> DniExistsForOtherClientAsync(string dni, int idCliente);
        Task<bool> CuitExistsForOtherClientAsync(string cuit, int idCliente);
        Task<bool> EmailExistsForOtherClientAsync(string email, int idCliente);
        Task<bool> EnterpriseExistsForOtherClientAsync(string enterprise, int idCliente);

        Task<Cliente> GetByIdAsync(int idCliente);
        Task<Cliente> CreateAsync(Cliente cliente);
        Task UpdateAsync(Cliente cliente);
        Task UpdateStatusAsync(int idCliente, DateOnly? fechaBaja);
        IQueryable<Cliente> ClientsQueryable(string searchTerm);
    }
}
