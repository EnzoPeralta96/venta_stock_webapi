using System.Collections.Generic;
using System.Threading.Tasks;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Category.CategoryRepository
{
    public interface ICategoryRepository
    {
        Task Create(Categorium category);
        Task Update(Categorium category);
        Task<bool> ExistsById(int idCategoria);
        Task<bool> ExistsByName(string nombre, int? excludeId = null);
        Task<List<Categorium>> GetAll();
        Task<Categorium?> GetById(int idCategoria);
        Task Delete(Categorium category);
    }
}