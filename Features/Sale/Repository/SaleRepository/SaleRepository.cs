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
                .Include(v => v.IdMotivoNcNavigation)
                .Include(v => v.DetalleVenta)
                    .ThenInclude(d => d.IdProductoNavigation)
                        .ThenInclude(p => p.IdUnidadMedidaNavigation)
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

            // Buscar el último código del día en ambas tablas
            var lastCodeVenta = await _context.Venta
                .Where(v => v.CodigoVenta.StartsWith(prefix))
                .OrderByDescending(v => v.CodigoVenta)
                .Select(v => v.CodigoVenta)
                .FirstOrDefaultAsync();

            var lastCodePendiente = await _context.VentaPendiente
                .Where(vp => vp.CodigoVenta.StartsWith(prefix))
                .OrderByDescending(vp => vp.CodigoVenta)
                .Select(vp => vp.CodigoVenta)
                .FirstOrDefaultAsync();

            int nextFromVenta = ParseSaleNumber(lastCodeVenta, prefix);
            int nextFromPendiente = ParseSaleNumber(lastCodePendiente, prefix);
            int nextNumber = Math.Max(nextFromVenta, nextFromPendiente);

            return $"{prefix}{nextNumber:D4}";
        }

        private static int ParseSaleNumber(string? lastCode, string prefix)
        {
            if (string.IsNullOrEmpty(lastCode)) return 1;
            var numberPart = lastCode.Substring(prefix.Length);
            return int.TryParse(numberPart, out var n) ? n + 1 : 1;
        }

        public Task<Ventum?> GetSaleWithDetailsAsync(int idVenta)
        {
            return _context.Venta
                .Include(v => v.DetalleVenta)
                    .ThenInclude(d => d.IdProductoNavigation)
                .Include(v => v.IdEstadoNavigation)
                .FirstOrDefaultAsync(v => v.IdVenta == idVenta);
        }

        public Task UpdateSaleStateAsync(int idVenta, int idEstado)
        {
            return _context.Venta
                .Where(v => v.IdVenta == idVenta)
                .ExecuteUpdateAsync(s => s.SetProperty(v => v.IdEstado, idEstado));
        }

        public Task AnnulSaleInDbAsync(int idVenta, int idEstado, int idMotivoNc, string? detalleNc)
        {
            return _context.Venta
                .Where(v => v.IdVenta == idVenta)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(v => v.IdEstado, idEstado)
                    .SetProperty(v => v.IdMotivoNc, idMotivoNc)
                    .SetProperty(v => v.DetalleNc, detalleNc));
        }
    }
}