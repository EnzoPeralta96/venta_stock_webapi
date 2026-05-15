using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Category.DTO;
using venta_stock_webapi.Features.Category.DTO;

namespace proyecto_venta_stock.Category.Services
{
    public interface ICategoryServices
    {
        Task<Result<bool>> Create(CreateCategoryDTO categoryDTO);
        Task<Result<bool>> Update(UpdateCategoryDTO categoryDTO);

        /*Task<Result<PagedList<CategoryDetailDTO>>> GetAllWithCategoryAndLocationPaged(
        int pageIndex,
        int pageSize,
        bool? activo = true,
        string? search = null
        );*/
        
        Task<Result<List<CategoryDetailDTO>>> GetAll();
        Task<Result<CategoryDetailDTO>> GetById(int idCategoria);

        Task<Result<bool>> Delete(int idCategoria);
    }
}
