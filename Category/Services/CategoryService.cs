using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Category.DTO;
using proyecto_venta_stock.Category.CategoryRepository;
using venta_stock_webapi.Shared.Paged;

namespace proyecto_venta_stock.Category.Services
{
    public class CategoryService : ICategoryServices
    {
        private readonly ILogger<CategoryService> _logger;
        private readonly IMapper _mapper;
        private readonly ICategoryRepository _categoryRepo;

        public CategoryService(ILogger<CategoryService> logger, IMapper mapper, ICategoryRepository categoryRepo)
        {
            _logger = logger;
            _mapper = mapper;
            _categoryRepo = categoryRepo;
        }

        public async Task<Result<bool>> Create(CategoryDetailDTO categoryDTO)
        {
            try
            {
                bool categoryExists = await _categoryRepo.ExistsByName(categoryDTO.Categoria);
                if (categoryExists)
                {
                    _logger.LogWarning("Category already exists");
                    return Result<bool>.Failure("category_already_exists");
                }


                var category = _mapper.Map<Categorium>(categoryDTO);
                await _categoryRepo.Create(category);
                return Result<bool>.Succes();
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inespetado:" + ex.ToString());
                return Result<bool>.Failure("error_inesperado_service");
            }
        }

        public async Task<Result<List<CategoryDetailDTO>>> GetAll()
        {
            try
            {
                var categorias = await _categoryRepo.GetAll();
                var dtos = _mapper.Map<List<CategoryDetailDTO>>(categorias);
                return Result<List<CategoryDetailDTO>>.Succes(dtos);
            }
            catch (System.Exception ex)
            {

                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<List<CategoryDetailDTO>>.Failure("error_inesperado");
            }
        }

        public async Task<Result<CategoryDetailDTO>> GetById(int idCategoria)
        {
            try
            {
                var category = await _categoryRepo.GetById(idCategoria);
                if (category == null)
                {
                    return Result<CategoryDetailDTO>.Failure("category_not_found");
                }
                var categoryDTO = _mapper.Map<CategoryDetailDTO>(category);
                return Result<CategoryDetailDTO>.Succes(categoryDTO);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<CategoryDetailDTO>.Failure("error_inesperado");
            }
        }

        public async Task<Result<bool>> Update(CategoryDetailDTO categoryDTO)
        {
            try
            {
                var existingCategory = await _categoryRepo.GetById(categoryDTO.IdCategoria);
                if (existingCategory == null)
                {
                    return Result<bool>.Failure("category_not_found");
                }
                bool nameInUse = await _categoryRepo.ExistsByName(categoryDTO.Categoria, categoryDTO.IdCategoria);
                if (nameInUse)
                {
                    return Result<bool>.Failure("category_name_in_use");
                }
                _mapper.Map(categoryDTO, existingCategory);

                await _categoryRepo.Update(existingCategory);
                return Result<bool>.Succes();
            }
            catch (System.Exception ex)
            {

                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure("error_inesperado");
            }
        }


        public async Task<Result<bool>> Delete(int idCategoria)
        {
            try
            {
                var existing = await _categoryRepo.GetById(idCategoria);
                if (existing == null) return Result<bool>.Failure("category_not_found");

                await _categoryRepo.Delete(existing);
                return Result<bool>.Succes(true); // <- importante: true
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Category in use");
                return Result<bool>.Failure("category_in_use");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected");
                return Result<bool>.Failure("error_inesperado");
            }
        }

        public Task<Result<PagedList<CategoryDetailDTO>>> GetAllWithCategoryAndLocationPaged(int pageIndex, int pageSize, bool? activo = true, string search = null)
        {
            throw new NotImplementedException();
        }
    }
}