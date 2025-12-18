using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Sale.Repository
{
    public class SaleRepository : ISaleRepository
    {
        private readonly VentaStockContext _context;

        public SaleRepository(VentaStockContext context)
        {
            _context = context;
        }

        public async Task<Ventum?> CreateSaleAsync(Ventum venta)
        {
            _context.Venta.Add(venta);
            await _context.SaveChangesAsync();
            return venta;
        }

        public async Task AddSaleItemsAsync(List<DetalleVentum> items)
        {
            await _context.DetalleVenta.AddRangeAsync(items);
            await _context.SaveChangesAsync();
        }


        public async Task<Ventum> GetSaleByIdAsync(int idVenta)
        {
            return await _context.Venta
                .Include(v => v.IdClienteNavigation)
                .Include(v => v.IdMedioPagoNavigation)
                .Include(v => v.IdEstadoNavigation)
                .Include(v => v.IdUsuarioNavigation)
                .Include(v => v.DetalleVenta)
                    .ThenInclude(d => d.IdProductoNavigation)
                .FirstOrDefaultAsync(v => v.IdVenta == idVenta);
        }

        public IQueryable<Ventum> SalesQueryable()
        {
            return _context.Venta
                .Include(v => v.IdClienteNavigation)
                .Include(v => v.IdMedioPagoNavigation)
                .Include(v => v.IdEstadoNavigation)
                .Include(v => v.IdUsuarioNavigation)
                .OrderByDescending(v => v.Fecha)
                .AsNoTracking();
        }

        public async Task<string> GenerateSaleCodeAsync()
        {
            // Formato: VENTA-YYYYMMDD-XXXX
            var today = DateTime.Now.ToString("yyyyMMdd");
            var prefix = $"VENTA-{today}-";
            
            // Obtener el último código del día
            var lastCode = await _context.Venta
                .Where(v => v.CodigoVenta.StartsWith(prefix))
                .OrderByDescending(v => v.CodigoVenta)
                .Select(v => v.CodigoVenta)
                .FirstOrDefaultAsync();

            int nextNumber = 1;
            if (!string.IsNullOrEmpty(lastCode))
            {
                var numberPart = lastCode.Substring(prefix.Length);
                if (int.TryParse(numberPart, out var currentNumber))
                {
                    nextNumber = currentNumber + 1;
                }
            }

            return $"{prefix}{nextNumber:D4}";
        }

        public async Task UpdateProductStockAsync(int idProducto, int quantitySold)
        {
            await _context.Productos
                .Where(p => p.IdProducto == idProducto)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(p => p.Stock, p => p.Stock - quantitySold)
                );
        }
    }
}