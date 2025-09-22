using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Product.DTO;

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


        public Task<bool> ExisteUbicacion(int idUbicacion)
        {
            return _dbContext.Ubicacions.AnyAsync(u => u.IdUbicacion == idUbicacion);
        }
        public Task<bool> CodigoBarraExists(CodigoBarraDTO codigoBarraDTO)
        {
            return _dbContext.CodigoBarras.AnyAsync(cb => cb.Codigo == codigoBarraDTO.Codigo);
        }
        public Task<Producto> GetById(int idProducto)
        {
            return _dbContext.Productos
                .Include(p => p.CodigoBarras) // Incluir los códigos de barra relacionados
                .FirstOrDefaultAsync(p => p.IdProducto == idProducto);
        }
        public Task<List<Producto>> GetAll()
        {
            return _dbContext.Productos
                .Include(p => p.CodigoBarras)
                .ToListAsync();
        }
        public Task<List<Producto>> GetAllWithCategoryAndUbication()
        {
            return _dbContext.Productos
                .Include(p => p.IdCategoriaNavigation)
                .Include(p => p.IdUbicacionNavigation)
                .ToListAsync();
        }
        public async Task Update(Producto nuevoProducto)
        {
            _dbContext.Productos.Update(nuevoProducto);
            await _dbContext.SaveChangesAsync();
        }
    }
}