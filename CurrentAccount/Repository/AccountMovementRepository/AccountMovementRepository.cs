using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

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
                     .Include(m => m.IdUsuarioAutorizaNavigation)
                     .ToListAsync();
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
                    .Where(t => t.IdMovimiento != 2 && t.IdMovimiento != 5)
                    .ToListAsync();
        }

        public Task<MovimientoCc> GetLastMovement(int clientId)
        {
            return _context.MovimientoCcs
                .AsNoTracking()
                .Where(m => m.IdCliente == clientId)
                .OrderByDescending(m => m.IdMovimiento)
                .FirstOrDefaultAsync();
        }
    }
}