using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Category.DTO;

namespace proyecto_venta_stock.Category.Services
{
    public interface ICategoryServices
    {
        Task<Result<bool>> Create(CategoryDetailDTO categoryDTO);
        Task<Result<bool>> Update(CategoryDetailDTO categoryDTO);
        Task<Result<List<CategoryBasicDTO>>> GetAll();
        Task<Result<CategoryBasicDTO>> GetById(int idCategoria);
    }
}
   