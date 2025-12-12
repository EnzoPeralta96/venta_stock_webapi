using proyecto_venta_stock.Models;

namespace venta_stock_webapi.CurrentAccount.Repository
{
    public interface IAccountMovementRepository
    {
        Task CreateMovement(MovimientoCc movimiento);
        Task<List<MovimientoCc>> GetMovements(int clientId);
        Task<MovimientoCc> GetLastMovement(int clientId);
        Task<string> GetDetailMovement(int IdTipoMovimiento);
        Task<List<TipoMovimiento>> GetMovementType();


    }
}