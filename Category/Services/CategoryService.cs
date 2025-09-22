using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Category.DTO;
using proyecto_venta_stock.Category.CategoryRepository;

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

        public async Task<Result<List<CategoryBasicDTO>>> GetAll()
        {
            try
            {
                var categorias = await _categoryRepo.GetAll();
                var dtos = _mapper.Map<List<CategoryBasicDTO>>(categorias);
                return Result<List<CategoryBasicDTO>>.Succes(dtos);
            }
            catch (System.Exception ex)
            {

                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<List<CategoryBasicDTO>>.Failure("error_inesperado");
            }
        }

        public async Task<Result<CategoryBasicDTO>> GetById(int idCategoria)
        {
            try
            {
                var category = await _categoryRepo.GetById(idCategoria);
                if (category == null)
                {
                    return Result<CategoryBasicDTO>.Failure("category_not_found");
                }
                var categoryDTO = _mapper.Map<CategoryBasicDTO>(category);
                return Result<CategoryBasicDTO>.Succes(categoryDTO);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<CategoryBasicDTO>.Failure("error_inesperado");
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
    }
}