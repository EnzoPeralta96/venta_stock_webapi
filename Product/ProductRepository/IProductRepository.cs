using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Product.DTO;

namespace proyecto_venta_stock.Product.ProductRepository
{
    public interface IProductRepository
    {
        Task Create(Producto producto);
        public Task<bool> Exists(string nombre, string marca);
        public Task<bool> CodigoBarraExists(CodigoBarraDTO codigoBarraDTO);

        public Task<Producto> GetById(int idProducto);
        public Task<List<Producto>> GetAll();
        public Task<List<Producto>> GetAllWithCategoryAndLocation();

        public Task Update(Producto nuevoProducto);
        
        Task Delete(Producto producto);
    }
}