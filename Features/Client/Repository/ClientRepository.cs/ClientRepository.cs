using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Client.Repository
{
    public class ClientRepository : IClientRepository
    {
        private readonly VentaStockContext _context;

        public ClientRepository(VentaStockContext context)
        {
            _context = context;
        }

        public Task<bool> DniExistsAsync(string dni)
        {
            return _context.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.Dni == dni && c.FechaBaja == null);
        }

        public Task<bool> CuitExistsAsync(string cuit)
        {
            return _context.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.Cuit == cuit && c.FechaBaja == null);
        }

        public Task<bool> EmailExistsAsync(string email)
        {
            return _context.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.Mail == email && c.FechaBaja == null);
        }

        public Task<bool> EnterpriseExistsAsync(string enterprise)
        {
            return _context.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.RazonSocial == enterprise && c.FechaBaja == null);
        }

        public Task<bool> DniExistsForOtherClientAsync(string dni, int idCliente)
        {
            return _context.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.Dni == dni && c.IdCliente != idCliente);
        }

        public Task<bool> CuitExistsForOtherClientAsync(string cuit, int idCliente)
        {
            return _context.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.Cuit == cuit && c.IdCliente != idCliente);
        }

        public Task<bool> EmailExistsForOtherClientAsync(string email, int idCliente)
        {
            return _context.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.Mail == email && c.IdCliente != idCliente);
        }

        public Task<Cliente> GetByIdAsync(int idCliente)
        {
            return _context.Clientes
                .AsNoTracking()
                .Where(c => c.IdCliente == idCliente)
                .Include(c => c.MovimientoCcs)
                .FirstOrDefaultAsync();
        }

        public async Task<Cliente> CreateAsync(Cliente cliente)
        {
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
            return cliente;
        }

        public async Task UpdateAsync(Cliente cliente)
        {
            _context.Clientes.Update(cliente);
            await _context.SaveChangesAsync();
        }

        public Task UpdateStatusAsync(int idCliente, DateOnly? fechaBaja)
        {
            return _context.Clientes
                .Where(c => c.IdCliente == idCliente)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(c => c.FechaBaja, fechaBaja)
                );
        }

        public IQueryable<Cliente> ClientsQueryable(string searchTerm)
        {
            var query = _context.Clientes.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.ToLower();
                query = query.Where(c =>
                    (c.Nombre != null && c.Nombre.ToLower().Contains(searchTerm)) ||
                    (c.Apellido != null && c.Apellido.ToLower().Contains(searchTerm)) ||
                    (c.RazonSocial != null && c.RazonSocial.ToLower().Contains(searchTerm)) ||
                    (c.Dni != null && c.Dni.Contains(searchTerm)) ||
                    (c.Cuit != null && c.Cuit.Contains(searchTerm)) ||
                    (c.Mail != null && c.Mail.ToLower().Contains(searchTerm)) ||
                    (c.Telefono != null && c.Telefono.Contains(searchTerm))
                );
            }

            return query;
        }

        public Task<bool> EnterpriseExistsForOtherClientAsync(string enterprise, int idCliente)
        {
            return _context.Clientes
              .AsNoTracking()
              .AnyAsync(c => c.RazonSocial == enterprise && c.IdCliente != idCliente);
        }

        public Task<bool> ExistsByIdAsync(int idCliente)
        {
            return _context.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.IdCliente == idCliente);
        }
        public async Task<CreditInfo?> ObtenerInfoCreditoAsync(int idCliente)
        {
            // SOLO obtener datos del último movimiento
            var ultimoMovimiento = await _context.MovimientoCcs
                .Where(m => m.IdCliente == idCliente)
                .OrderByDescending(m => m.Fecha)
                .ThenByDescending(m => m.IdMovimiento)
                .FirstOrDefaultAsync();

            if (ultimoMovimiento == null)
                return null;

            // Retornar datos puros (sin validación)
            return new CreditInfo
            {
                SaldoActual = ultimoMovimiento.SaldoActual ?? 0,
                LimiteCuenta = ultimoMovimiento.LimiteCuenta ?? 0
            };
        }
    }
}