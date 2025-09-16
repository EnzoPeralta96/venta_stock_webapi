using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Product.ProductRepository
{
    public class ProductRepository : IProductRepository
    {
        private readonly VentaStockContext _dbContext;
        public ProductRepository(VentaStockContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Create(Producto producto)
        {
            await _dbContext.AddAsync(producto);
            await _dbContext.SaveChangesAsync();
        }

        public Task<bool> Exists(string nombre, string marca)
        {
            return _dbContext.Productos.AnyAsync(p => p.Nombre == nombre && p.Marca == marca);
        }

        public Task<bool> ExisteCategoria(int idCategoria)
        {
            return _dbContext.Categoria.AnyAsync(c => c.IdCategoria == idCategoria);
        }

        public Task<bool> ExisteUbicacion(int idUbicacion)
        {
            return _dbContext.Ubicacions.AnyAsync(u => u.IdUbicacion == idUbicacion);
        }
    }
}