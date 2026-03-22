using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using venta_stock_webapi.CurrentAccount.DTO.MovementDTO;
using venta_stock_webapi.Shared.Paged;

namespace venta_stock_webapi.CurrentAccount.Repository
{
    public class AccountMovementRepository : IAccountMovementRepository
    {
        private readonly VentaStockContext _context;

        public AccountMovementRepository(VentaStockContext context)
        {
            _context = context;
        }

        public async Task CreateMovement(MovimientoCc movimiento)
        {
            await _context.MovimientoCcs.AddAsync(movimiento);
            await _context.SaveChangesAsync();
        }

        public Task<List<MovimientoCc>> GetMovements(int clientId)
        {
            return _context.MovimientoCcs
                     .AsNoTracking()
                     .Where(m => m.IdCliente == clientId)
                     .Include(m => m.IdEstadoNavigation)
                     .Include(m => m.IdTipoMovimientoNavigation)
                     .Include(m => m.IdUsuarioRegistraNavigation)
                     .Include(m => m.IdVentaNavigation)
                         .ThenInclude(v => v.IdEstadoNavigation)
                     .Include(m => m.IdMotivoNdNavigation)
                     .Include(m => m.IdMotivoNcNavigation)
                     .OrderBy(m => m.Fecha)
                        .ThenBy(m => m.IdMovimiento)
                     .ToListAsync();
        }

        public async Task<(MovimientoCc opening, MovimientoCc latest)> GetAccountSummaryAsync(int clientId)
        {
            var includes = _context.MovimientoCcs
                .AsNoTracking()
                .Where(m => m.IdCliente == clientId)
                .Include(m => m.IdEstadoNavigation)
                .Include(m => m.IdTipoMovimientoNavigation)
                .Include(m => m.IdUsuarioRegistraNavigation)
                .Include(m => m.IdVentaNavigation)
                    .ThenInclude(v => v.IdEstadoNavigation)
                .Include(m => m.IdVentaNavigation)
                    .ThenInclude(v => v.IdMotivoNcNavigation)
                .Include(m => m.IdMotivoNdNavigation)
                .Include(m => m.IdMotivoNcNavigation);

            var opening = await includes
                .Where(m => m.IdTipoMovimiento == 2)
                .FirstOrDefaultAsync();

            var latest = await includes
                .Where(m => m.IdTipoMovimiento != 2)
                .OrderByDescending(m => m.Fecha)
                .ThenByDescending(m => m.IdMovimiento)
                .FirstOrDefaultAsync();

            return (opening, latest);
        }

        public Task<PagedList<MovimientoCc>> GetMovementsPagedAsync(
            int clientId,
            int pageIndex,
            int pageSize,
            string searchTerm,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            int? idTipoMovimiento)
        {
            var query = _context.MovimientoCcs
                .AsNoTracking()
                .Where(m => m.IdCliente == clientId && m.IdTipoMovimiento != 2)
                .Include(m => m.IdEstadoNavigation)
                .Include(m => m.IdTipoMovimientoNavigation)
                .Include(m => m.IdUsuarioRegistraNavigation)
                .Include(m => m.IdVentaNavigation)
                    .ThenInclude(v => v.IdEstadoNavigation)
                .Include(m => m.IdVentaNavigation)
                    .ThenInclude(v => v.IdMotivoNcNavigation)
                .Include(m => m.IdMotivoNdNavigation)
                .Include(m => m.IdMotivoNcNavigation)
                .OrderByDescending(m => m.Fecha)
                .ThenByDescending(m => m.IdMovimiento)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(m =>
                    (m.Detalle != null && m.Detalle.ToLower().Contains(term)) ||
                    (m.IdVentaNavigation != null && m.IdVentaNavigation.CodigoVenta.ToLower().Contains(term)));
            }

            if (fechaDesde.HasValue)
                query = query.Where(m => m.Fecha >= fechaDesde.Value);

            if (fechaHasta.HasValue)
                query = query.Where(m => m.Fecha <= fechaHasta.Value.Date.AddDays(1).AddTicks(-1));

            if (idTipoMovimiento.HasValue)
                query = query.Where(m => m.IdTipoMovimiento == idTipoMovimiento.Value);

            return PagedList<MovimientoCc>.CreateAsync(query, pageIndex, pageSize);
        }

        public Task<string> GetDetailMovement(int IdTipoMovimiento)
        {
            return _context.TipoMovimientos
                .Where(c => c.IdMovimiento == IdTipoMovimiento)
                .Select(c => c.Accion)
                .FirstOrDefaultAsync();
        }

        public Task<List<TipoMovimiento>> GetMovementType()
        {
            return _context.TipoMovimientos
                    .Where(t => t.IdMovimiento != 2 && t.IdMovimiento != 5 && t.IdMovimiento != 7)
                    .ToListAsync();
        }

        public Task<MovimientoCc> GetLastMovement(int clientId)
        {
            return _context.MovimientoCcs
                .AsNoTracking()
                .Where(m => m.IdCliente == clientId)
                .OrderByDescending(m => m.Fecha)
                .ThenByDescending(m => m.IdMovimiento)
                .FirstOrDefaultAsync();
        }


        public Task<List<MovimientoCc>> GetUnpaidConsumptions(int clientId)
        {
            return _context.MovimientoCcs
                .Where(m => m.IdCliente == clientId
                         && m.IdTipoMovimiento == 5  // MOVIMIENTO_CC
                         && (m.MontoPagado == null || m.MontoPagado < m.Importe))
                .OrderBy(m => m.Fecha)
                .ThenBy(m => m.IdMovimiento)
                .ToListAsync();
        }

        public Task<MovimientoCc> GetConsumptionByVentaId(int idVenta)
        {
            return _context.MovimientoCcs
                .Where(m => m.IdVenta == idVenta && m.IdTipoMovimiento == 5)
                .FirstOrDefaultAsync();
        }

        public Task<MovimientoCc> GetMovementById(int idMovimiento)
        {
            return _context.MovimientoCcs
                .AsNoTracking()
                .Include(m => m.IdClienteNavigation)
                .Include(m => m.IdTipoMovimientoNavigation)
                .Include(m => m.IdUsuarioRegistraNavigation)
                .Include(m => m.IdVentaNavigation)
                .Include(m => m.IdMotivoNdNavigation)
                .Include(m => m.IdMotivoNcNavigation)
                .FirstOrDefaultAsync(m => m.IdMovimiento == idMovimiento);
        }

        public async Task UpdateMovimientos(List<MovimientoCc> movimientos)
        {
            _context.MovimientoCcs.UpdateRange(movimientos);
            await _context.SaveChangesAsync();
        }

        public Task<MovimientoCc> GetPaymentMovementById(int idMovimiento)
        {
            return _context.MovimientoCcs
                .Where(m => m.IdMovimiento == idMovimiento
                         && (m.IdTipoMovimiento == 6 || m.IdTipoMovimiento == 8 || m.IdTipoMovimiento == 11))
                .FirstOrDefaultAsync();
        }

        public Task<List<MovimientoCc>> GetAllConsumptionsTracked(int clientId)
        {
            return _context.MovimientoCcs
                .Where(m => m.IdCliente == clientId && m.IdTipoMovimiento == 5)
                .OrderBy(m => m.Fecha)
                .ThenBy(m => m.IdMovimiento)
                .ToListAsync();
        }

        public Task<List<MovimientoCc>> GetAllValidPayments(int clientId)
        {
            return _context.MovimientoCcs
                .AsNoTracking()
                .Where(m => m.IdCliente == clientId
                         && (m.IdTipoMovimiento == 6 || m.IdTipoMovimiento == 8 || m.IdTipoMovimiento == 11)
                         && !m.EsAnulado)
                .OrderBy(m => m.Fecha)
                .ThenBy(m => m.IdMovimiento)
                .ToListAsync();
        }

        public Task<DateTime?> GetAccountOpeningDateAsync(int clientId)
        {
            return _context.MovimientoCcs
                .AsNoTracking()
                .Where(m => m.IdCliente == clientId && m.IdTipoMovimiento == 2)
                .Select(m => m.Fecha)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Recupera una lista de pagos de ventas pendientes para un cliente específico.
        /// </summary>
        /// <remarks>
        /// Este método consulta todas las ventas aprobadas (Estado = 2) pagadas a través de cuenta corriente (IdMedioPago = 2)
        /// para el cliente especificado. Calcula el importe total pagado mediante movimientos de cuenta de tipo
        /// PAGO_FACTURA (pago a factura, IdTipoMovimiento = 8) y determina el saldo pendiente.
        /// Solo se devuelven las ventas con un saldo restante mayor a cero, ordenadas por fecha en orden descendente.
        /// </remarks>
        /// <param name="clientId">El identificador único del cliente.</param>
        /// <returns>
        /// Una tarea que representa la operación asíncrona. El resultado de la tarea contiene una lista de objetos
        /// <see cref="PendingSalePaymentDTO"/> que representan las ventas pendientes con su estado de pago. /// </returns>
        public async Task<List<PendingSalePaymentDTO>> GetPendingSalesForPayment(int clientId)
        {
            // Obtener todas las ventas del cliente pagadas con cuenta corriente (IdMedioPago = 2)
            var salesWithPayments = await _context.Venta
                .AsNoTracking()
                .Where(v => v.IdCliente == clientId && v.IdMedioPago == 2 && v.IdEstado == 2) // Estado 2 = Aprobado
                .GroupJoin(
                    _context.MovimientoCcs.Where(m => m.IdTipoMovimiento == 8 && !m.EsAnulado), // Tipo 8 = PAGO_FACTURA (no anulados)
                    venta => venta.IdVenta,
                    movimiento => movimiento.IdVenta,
                    (venta, movimientos) => new
                    {
                        Venta = venta,
                        TotalPagado = movimientos.Sum(m => m.Importe ?? 0)
                    }
                )
                .Select(x => new PendingSalePaymentDTO
                {
                    IdVenta = x.Venta.IdVenta,
                    CodigoVenta = x.Venta.CodigoVenta,
                    Fecha = x.Venta.Fecha,
                    TotalVenta = x.Venta.Total ?? 0,
                    TotalPagado = x.TotalPagado,
                    SaldoPendiente = (x.Venta.Total ?? 0) - x.TotalPagado
                })
                .Where(x => x.SaldoPendiente > 0)
                .OrderByDescending(x => x.Fecha)
                .ToListAsync();

            return salesWithPayments;
        }

        public Task<Ventum?> GetSaleByIdAndClient(int idVenta, int idCliente)
        {
            return _context.Venta
                .AsNoTracking()
                .FirstOrDefaultAsync(v => v.IdVenta == idVenta && v.IdCliente == idCliente);
        }

        public async Task<List<(int IdCliente, decimal SaldoActual)>> GetClientsWithDebt()
        {
            // Obtener el último IdMovimiento por cliente
            var latestIds = await _context.MovimientoCcs
                .AsNoTracking()
                .GroupBy(m => m.IdCliente)
                .Select(g => g.Max(m => m.IdMovimiento))
                .ToListAsync();

            // Filtrar los que tienen deuda
            var result = await _context.MovimientoCcs
                .AsNoTracking()
                .Where(m => latestIds.Contains(m.IdMovimiento) && m.SaldoActual > 0 && m.IdCliente != null)
                .Select(m => new { IdCliente = m.IdCliente!.Value, SaldoActual = m.SaldoActual ?? 0 })
                .ToListAsync();

            return result.Select(x => (x.IdCliente, x.SaldoActual)).ToList();
        }

        public Task<bool> HasInterestAppliedThisMonth(int clientId)
        {
            var now = DateTime.Now;
            return _context.MovimientoCcs
                .AsNoTracking()
                .AnyAsync(m =>
                    m.IdCliente == clientId &&
                    m.IdTipoMovimiento == 3 &&
                    m.Fecha.HasValue &&
                    m.Fecha.Value.Month == now.Month &&
                    m.Fecha.Value.Year == now.Year &&
                    m.IdMotivoNdNavigation != null &&
                    m.IdMotivoNdNavigation.Nombre == "Interés por mora");
        }

        public Task<MovimientoCc?> GetConsumptionByVentaIdTracked(int idVenta)
        {
            return _context.MovimientoCcs
                .Where(m => m.IdVenta == idVenta && m.IdTipoMovimiento == 5)
                .FirstOrDefaultAsync();
        }
    }
}