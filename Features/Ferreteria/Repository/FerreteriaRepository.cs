using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Features.Ferreteria.Repository
{
    public class FerreteriaRepository : IFerreteriaRepository
    {
        private readonly VentaStockContext _context;

        public FerreteriaRepository(VentaStockContext context)
        {
            _context = context;
        }

        public async Task<Models.Ferreteria?> GetAsync()
        {
            // Se asume una sola fila de configuración de ferretería
            return await _context.Ferreterias
                .AsNoTracking()
                .FirstOrDefaultAsync();
        }

        public async Task UpdateAsync(Models.Ferreteria ferreteria)
        {
            ferreteria.FechaActualizacion = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);
            _context.Ferreterias.Update(ferreteria);
            await _context.SaveChangesAsync();
        }
    }
}
