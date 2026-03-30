using AutoMapper;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Category.DTO;
using proyecto_venta_stock.Category.CategoryRepository;
using proyecto_venta_stock.Message;
using venta_stock_webapi.Features.Category.DTO;

namespace proyecto_venta_stock.Category.Services
{
    public class CategoryService : ICategoryServices
    {
        private readonly ILogger<CategoryService> _logger;
        private readonly IMapper _mapper;
        private readonly ICategoryRepository _categoryRepo;
        private readonly VentaStockContext _context;

        public CategoryService(ILogger<CategoryService> logger, IMapper mapper, ICategoryRepository categoryRepo, VentaStockContext context)
        {
            _logger = logger;
            _mapper = mapper;
            _categoryRepo = categoryRepo;
            _context = context;
        }

        public async Task<Result<bool>> Create(CreateCategoryDTO categoryDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                bool categoryExists = await _categoryRepo.ExistsByName(categoryDTO.Categoria);

                if (categoryExists)
                {
                    _logger.LogWarning("Category already exists");
                    return Result<bool>.Failure(CategoryErrorCode.category_already_exists);
                }

                var category = _mapper.Map<Categorium>(categoryDTO);

                await _categoryRepo.Create(category);

                await transaction.CommitAsync();
                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure(CategoryErrorCode.error_inesperado);
            }
        }

        public async Task<Result<List<CategoryDetailDTO>>> GetAll()
        {
            try
            {
                var categorias = await _categoryRepo.GetAll();

                var dtos = _mapper.Map<List<CategoryDetailDTO>>(categorias);

                return Result<List<CategoryDetailDTO>>.Success(dtos);
            }
            catch (Exception ex)
            {

                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<List<CategoryDetailDTO>>.Failure(CategoryErrorCode.error_inesperado);
            }
        }

        public async Task<Result<CategoryDetailDTO>> GetById(int idCategoria)
        {
            try
            {
                var category = await _categoryRepo.GetById(idCategoria);
                if (category == null)
                {
                    return Result<CategoryDetailDTO>.Failure(CategoryErrorCode.category_not_found);
                }
                var categoryDTO = _mapper.Map<CategoryDetailDTO>(category);
                return Result<CategoryDetailDTO>.Success(categoryDTO);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<CategoryDetailDTO>.Failure(CategoryErrorCode.error_inesperado);
            }
        }

        public async Task<Result<bool>> Update(UpdateCategoryDTO categoryDTO)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingCategory = await _categoryRepo.GetById(categoryDTO.IdCategoria);

                if (existingCategory is null)
                    return Result<bool>.Failure(CategoryErrorCode.category_not_found);

                bool nameInUse = await _categoryRepo.ExistsByName(categoryDTO.Categoria, categoryDTO.IdCategoria);

                if (nameInUse)
                    return Result<bool>.Failure(CategoryErrorCode.category_name_in_use);

                _mapper.Map(categoryDTO, existingCategory);

                await _categoryRepo.Update(existingCategory);

                await transaction.CommitAsync();
                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure(CategoryErrorCode.error_inesperado);
            }
        }


        public async Task<Result<bool>> Delete(int idCategoria)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existing = await _categoryRepo.GetById(idCategoria);

                if (existing is null)
                    return Result<bool>.Failure(CategoryErrorCode.category_not_found);

                await _categoryRepo.Delete(existing);

                await transaction.CommitAsync();
                return Result<bool>.Success(true);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException ex)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Category in use");
                return Result<bool>.Failure(CategoryErrorCode.category_in_use);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Unexpected");
                return Result<bool>.Failure(CategoryErrorCode.error_inesperado);
            }
        }
    }
}