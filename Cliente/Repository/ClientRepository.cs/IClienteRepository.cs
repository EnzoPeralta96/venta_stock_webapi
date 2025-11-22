namespace venta_stock_webapi.Cliente.Repository
{
    public interface IClienteRepository
    {
        Task<bool> DniExistsAsync(string dni);
        Task<bool> CuitExistsAsync(string cuit);
        Task<bool> EmailExistsAsync(string email);
        Task<proyecto_venta_stock.Models.Cliente?> GetByIdAsync(int idCliente);
        Task<proyecto_venta_stock.Models.Cliente> CreateAsync(proyecto_venta_stock.Models.Cliente cliente);
    }
}
