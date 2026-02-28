using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
/* using proyecto_venta_stock.Models.Proveedor; */

namespace proyecto_venta_stock.Proveedor.ProveedorRepository
{
    public class ProveedorRepository : IProveedorRepository
    {
        private readonly VentaStockContext _dbContext;

        public ProveedorRepository(VentaStockContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Create(Models.Proveedor proveedor)
        {
            await _dbContext.Set<Models.Proveedor>().AddAsync(proveedor);
            await _dbContext.SaveChangesAsync();
        }

        public async Task Update(Models.Proveedor proveedor)
        {
            _dbContext.Set<Models.Proveedor>().Update(proveedor);
            await _dbContext.SaveChangesAsync();
        }

        public Task<Models.Proveedor?> GetById(int idProveedor)
        {
            return _dbContext.Set<Models.Proveedor>()
                .Include(p => p.ListaPrecios)
                .FirstOrDefaultAsync(p => p.IdProveedor == idProveedor);
        }

        public Task<List<Models.Proveedor>> GetAll()
        {
            return _dbContext.Set<Models.Proveedor>()
                .Include(p => p.ListaPrecios)
                .OrderBy(p => p.Proveedor1)
                .ToListAsync();
        }

        public Task<bool> Exists(string nombre, int? excludeId = null)
        {
            var q = _dbContext.Set<Models.Proveedor>().AsQueryable();

            if (excludeId.HasValue)
                q = q.Where(p => p.IdProveedor != excludeId.Value);

            return q.AnyAsync(p => p.Proveedor1 == nombre);
        }

        public async Task<bool> IsInUse(int idProveedor)
        {
            // Tiene listas?
            var hasListas = await _dbContext.Set<ListaPrecio>()
                .AnyAsync(lp => lp.IdProveedor == idProveedor);

            if (hasListas) return true;

            // Tiene compras?
            var hasCompras = await _dbContext.Set<Compra>()
                .AnyAsync(c => c.IdProveedor == idProveedor);

            return hasCompras;
        }

        public async Task Delete(Models.Proveedor proveedor)
        {
            _dbContext.Set<Models.Proveedor>().Remove(proveedor);
            await _dbContext.SaveChangesAsync();
        }
    }
}