using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Client.Repository
{
    public interface IAccountMovementRepository
    {
        Task CreateAccount(MovimientoCc movimiento);
    }
}