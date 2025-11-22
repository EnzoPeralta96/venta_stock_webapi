using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Cliente.Repository
{
    public class AccountMovementRepository : IAccountMovementRepository
    {
        private readonly VentaStockContext _context;

        public AccountMovementRepository(VentaStockContext context)
        {
            _context = context;
        }

        public async Task CreateAccount(MovimientoCc movimiento)
        {
            await _context.MovimientoCcs.AddAsync(movimiento);
            await _context.SaveChangesAsync();
        }
    }
}