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
                .Where(p => p.Activo)
                .OrderBy(p => p.Proveedor1)
                .ToListAsync();
        }

        public IQueryable<Models.Proveedor> ProveedoresQueryable(string searchTerm)
        {
            var query = _dbContext.Set<Models.Proveedor>()
                .AsNoTracking()
                .OrderBy(p => p.FechaBaja != null)
                    .ThenBy(p => p.Proveedor1)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p =>
                    p.Proveedor1.ToLower().Contains(searchTerm.ToLower()) ||
                    (p.Telefono != null && p.Telefono.Contains(searchTerm)) ||
                    (p.Direccion != null && p.Direccion.ToLower().Contains(searchTerm.ToLower()))
                );
            }

            return query;
        }

        public Task<bool> Exists(string nombre, int? excludeId = null)
        {
            var q = _dbContext.Set<Models.Proveedor>()
                .Where(p => p.FechaBaja == null)
                .AsQueryable();

            if (excludeId.HasValue)
                q = q.Where(p => p.IdProveedor != excludeId.Value);

            return q.AnyAsync(p => p.Proveedor1 == nombre);
        }

        public async Task Delete(Models.Proveedor proveedor)
        {
            proveedor.FechaBaja = DateTime.Now;
            proveedor.Activo = false;
            _dbContext.Set<Models.Proveedor>().Update(proveedor);
            await _dbContext.SaveChangesAsync();
        }
    }
}