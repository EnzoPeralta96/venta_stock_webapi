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
        Task<bool> ExistsByIdAsync(int idCliente);
        Task<Cliente> GetByIdAsync(int idCliente);
        Task<Cliente> CreateAsync(Cliente cliente);
        Task UpdateAsync(Cliente cliente);
        Task UpdateStatusAsync(int idCliente, DateOnly? fechaBaja);
        IQueryable<Cliente> ClientsQueryable(string searchTerm);

        /// Obtiene información de crédito del cliente (solo datos)
        /// CreditInfo con saldo y límite actuales,
        /// o null si el cliente no tiene cuenta corriente
        Task<CreditInfo?> ObtenerInfoCreditoAsync(int idCliente);

    }
    public class CreditInfo
    {
        public decimal SaldoActual { get; set; }
        public decimal LimiteCuenta { get; set; }

        // Propiedades calculadas (helper)
        public decimal LimiteDisponible => LimiteCuenta - SaldoActual;
        public decimal PorcentajeUso => LimiteCuenta > 0
            ? (SaldoActual / LimiteCuenta) * 100
            : 0;
    }
}
