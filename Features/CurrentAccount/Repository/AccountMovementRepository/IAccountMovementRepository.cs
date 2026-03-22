using proyecto_venta_stock.Models;
using venta_stock_webapi.CurrentAccount.DTO.MovementDTO;
using venta_stock_webapi.Shared.Paged;

namespace venta_stock_webapi.CurrentAccount.Repository
{
    public interface IAccountMovementRepository
    {
        Task CreateMovement(MovimientoCc movimiento);
        Task<List<MovimientoCc>> GetMovements(int clientId);
        Task<MovimientoCc> GetLastMovement(int clientId);

        // Devuelve el movimiento alta_cliente (tipo 2) y el último movimiento operativo del cliente.
        Task<(MovimientoCc opening, MovimientoCc latest)> GetAccountSummaryAsync(int clientId);

        // Devuelve movimientos paginados con filtros opcionales (excluye alta_cliente).
        Task<PagedList<MovimientoCc>> GetMovementsPagedAsync(
            int clientId,
            int pageIndex,
            int pageSize,
            string searchTerm,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            int? idTipoMovimiento);
        Task<string> GetDetailMovement(int IdTipoMovimiento);
        Task<List<TipoMovimiento>> GetMovementType();
        Task<List<PendingSalePaymentDTO>> GetPendingSalesForPayment(int clientId);

        // Recupera consumos (movimiento_cc) con pago incompleto, ordenados por fecha ASC.
        // Se usa para la allocación secuencial del pago_global.
        Task<List<MovimientoCc>> GetUnpaidConsumptions(int clientId);

        // Busca el movimiento de consumo (movimiento_cc) vinculado a una venta específica.
        // Se usa al registrar un pago_factura para actualizar su MontoPagado.
        Task<MovimientoCc> GetConsumptionByVentaId(int idVenta);

        // Recupera un movimiento por su ID incluyendo navegación al cliente.
        // Se usa para generar el comprobante PDF de pago.
        Task<MovimientoCc> GetMovementById(int idMovimiento);

        // Persiste los cambios de MontoPagado en un batch de movimientos.
        Task UpdateMovimientos(List<MovimientoCc> movimientos);

        // Busca un movimiento de pago (tipo 6 o 8) por su ID, con tracking para poder modificarlo.
        // Retorna null si no existe o si no es un pago.
        Task<MovimientoCc> GetPaymentMovementById(int idMovimiento);

        // Devuelve todos los consumos (tipo 5) del cliente ordenados por fecha ASC, con tracking.
        Task<List<MovimientoCc>> GetAllConsumptionsTracked(int clientId);

        // Devuelve todos los pagos válidos (tipo 6 u 8, EsAnulado == false) del cliente, AsNoTracking.
        Task<List<MovimientoCc>> GetAllValidPayments(int clientId);

        // Busca una venta por ID que pertenezca al cliente indicado. Retorna null si no existe.
        Task<Ventum?> GetSaleByIdAndClient(int idVenta, int idCliente);

        // Devuelve el último movimiento de cada cliente cuyo SaldoActual > 0.
        Task<List<(int IdCliente, decimal SaldoActual)>> GetClientsWithDebt();

        // Verifica si ya existe una ND de tipo NOTA_DEBITO con motivo "Interés por mora"
        // para el cliente en el mes y año actual.
        Task<bool> HasInterestAppliedThisMonth(int clientId);

        // Busca el movimiento de consumo (tipo 5) vinculado a una venta, con tracking.
        // Se usa al anular una venta para recomputar MontoPagado.
        Task<MovimientoCc?> GetConsumptionByVentaIdTracked(int idVenta);

        // Retorna la fecha del movimiento de apertura de CC (tipo 2) del cliente.
        // Se usa para excluir de mora a clientes cuya CC abrió después del vencimiento del mes actual.
        Task<DateTime?> GetAccountOpeningDateAsync(int clientId);
    }
}