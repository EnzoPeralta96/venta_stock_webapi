using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;

namespace proyecto_venta_stock.Proveedor.ProveedorRepository
{
    public class ProveedorRepository : IProveedorRepository
    {
        private readonly VentaStockContext _dbContext;

        public ProveedorRepository(VentaStockContext dbContext)
        {
            _dbContext = dbContext;
        }

        public void Create(Models.Proveedor proveedor)
        {
            _dbContext.Proveedors.Add(proveedor);
        }

        public void Update(Models.Proveedor proveedor)
        {
            _dbContext.Proveedors.Update(proveedor);
        }

        public Task<Models.Proveedor?> GetById(int idProveedor)
        {
            return _dbContext.Proveedors
                .Include(p => p.ListaPrecios)
                .FirstOrDefaultAsync(p => p.IdProveedor == idProveedor);
        }

        public Task<List<Models.Proveedor>> GetAll()
        {
            return _dbContext.Proveedors
                .AsNoTracking()
                .Include(p => p.ListaPrecios)
                .Where(p => p.Activo)
                .OrderBy(p => p.Proveedor1)
                .ToListAsync();
        }

        public IQueryable<Models.Proveedor> ProveedoresQueryable(string searchTerm)
        {
            var query = _dbContext.Proveedors
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
            var q = _dbContext.Proveedors
                .AsNoTracking()
                .Where(p => p.FechaBaja == null)
                .AsQueryable();

            if (excludeId.HasValue)
                q = q.Where(p => p.IdProveedor != excludeId.Value);

            return q.AnyAsync(p => p.Proveedor1 == nombre);
        }

        public void Delete(Models.Proveedor proveedor)
        {
            proveedor.FechaBaja = DateTime.Now;
            proveedor.Activo = false;
            _dbContext.Proveedors.Update(proveedor);
        }

        public Task SaveChangesAsync()
        {
            return _dbContext.SaveChangesAsync();
        }

        public Task<bool> Exists(int idProveedor)
        {
            return _dbContext.Proveedors
                .AsNoTracking()
                .AnyAsync(p => p.IdProveedor == idProveedor && p.Activo);
        }
    }
}