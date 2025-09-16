using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Product.DTO;

namespace proyecto_venta_stock.Product.Services
{
    public interface IProductServices
    {
        Task<Result<bool>> Create(ProductDTO productDTO);
    }
}