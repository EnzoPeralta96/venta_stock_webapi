using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Category.DTO;

namespace proyecto_venta_stock.Category.CategoryRepository
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly VentaStockContext _dbContext;

        public CategoryRepository(VentaStockContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Create(Categorium category)
        {
            await _dbContext.AddAsync(category);
            await _dbContext.SaveChangesAsync();
        }
        public Task Update(Categorium category)
        {
            _dbContext.Categoria.Update(category);
            return _dbContext.SaveChangesAsync();
        }
        public async Task<bool> ExistsById(int idCategoria)
        {
            return await _dbContext.Categoria.AnyAsync(c => c.IdCategoria == idCategoria);
        }
        public Task<bool> ExistsByName(string nombre, int? excludeId = null)
        {
            var normalized = nombre.Trim().ToUpper();
            return _dbContext.Categoria
                .AnyAsync(c =>
                    c.Categoria.ToUpper() == normalized &&
                    (excludeId == null || c.IdCategoria != excludeId.Value));
        }

        /* Consultar si hago uso del AsNoTracking */
        public async Task<List<Categorium>> GetAll()
        {
            return await _dbContext.Categoria.AsNoTracking().ToListAsync();
        }

        public async Task<Categorium?> GetById(int idCategoria)
        {
            return await _dbContext.Categoria.AsNoTracking().FirstOrDefaultAsync(c => c.IdCategoria == idCategoria);
        }

        public async Task Delete(Categorium category)
        {
            _dbContext.Categoria.Remove(category);
            await _dbContext.SaveChangesAsync();
        }
        
    }
}
