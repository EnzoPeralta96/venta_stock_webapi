using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;

namespace venta_stock_webapi.Cliente.Repository
{
    public class ClienteRepository : IClienteRepository
    {
        private readonly VentaStockContext _context;

        public ClienteRepository(VentaStockContext context)
        {
            _context = context;
        }

        public async Task<bool> DniExistsAsync(string dni)
        {
            return await _context.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.Dni == dni);
        }

        public async Task<bool> CuitExistsAsync(string cuit)
        {
            return await _context.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.Cuit == cuit);
        }

        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Clientes
                .AsNoTracking()
                .AnyAsync(c => c.Mail == email);
        }

        public async Task<proyecto_venta_stock.Models.Cliente?> GetByIdAsync(int idCliente)
        {
            return await _context.Clientes
                .Include(c => c.MovimientoCcs)
                .FirstOrDefaultAsync(c => c.IdCliente == idCliente);
        }

        public async Task<proyecto_venta_stock.Models.Cliente> CreateAsync(proyecto_venta_stock.Models.Cliente cliente)
        {
            _context.Clientes.Add(cliente);
            await _context.SaveChangesAsync();
            return cliente;
        }
    }
}