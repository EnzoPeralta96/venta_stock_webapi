using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Product.DTO;

namespace proyecto_venta_stock.Product.ProductRepository
{
    public interface IProductRepository
    {
        Task Create(Producto producto);
        public Task<bool> Exists(string nombre, string marca);
        public Task<bool> ExisteCategoria(int idCategoria);

        public Task<bool> ExisteUbicacion(int idUbicacion);
        public Task<bool> CodigoBarraExists(CodigoBarraDTO codigoBarraDTO);
    }
}