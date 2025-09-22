using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Product.DTO;
using proyecto_venta_stock.Product.ProductRepository;
using proyecto_venta_stock.Category.CategoryRepository;
namespace proyecto_venta_stock.Product.Services
{
    public class ProductServices : IProductServices
    {
        private readonly ILogger<ProductServices> _logger;
        private readonly IProductRepository _productRepository;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IMapper _mapper;
        public ProductServices(IProductRepository productRepository, ILogger<ProductServices> logger, IMapper mapper, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _categoryRepository = categoryRepository;
            _logger = logger;
            _mapper = mapper;
        }
        public async Task<Result<bool>> Create(ProductDTO productDTO)
        {
            try
            {
                bool productExists = await _productRepository.Exists(productDTO.Nombre, productDTO.Marca);


                if (productExists) return Result<bool>.Failure("product_name_in_use");

                if (productDTO.IdCategoria == null || !await _categoryRepository.ExistsById(productDTO.IdCategoria.Value))
                    return Result<bool>.Failure("categoria_invalida");

                if (productDTO.IdUbicacion == null || !await _productRepository.ExisteUbicacion(productDTO.IdUbicacion.Value))
                    return Result<bool>.Failure("ubicacion_invalida");

                foreach (var cb in productDTO.CodigoBarras)
                {
                    if (await _productRepository.CodigoBarraExists(cb))
                        return Result<bool>.Failure($"codigo_barra_duplicado: {cb.Codigo}");
                }

                var product = _mapper.Map<Producto>(productDTO);

                await _productRepository.Create(product);

                return Result<bool>.Succes();
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure("error_inesperado");
            }
        }

        public async Task<Result<bool>> Update(ProductDTO productDTO)
        {
            try
            {
                // Verificar si el producto existe
                var existingProduct = await _productRepository.GetById(productDTO.IdProducto);
                if (existingProduct == null) return Result<bool>.Failure("product_not_found");

                // Verificar si el nuevo nombre y marca ya están en uso por otro producto
                bool productExists = await _productRepository.Exists(productDTO.Nombre, productDTO.Marca);
                if (productExists && (existingProduct.Nombre != productDTO.Nombre || existingProduct.Marca != productDTO.Marca))
                    return Result<bool>.Failure("product_name_in_use");

                if (productDTO.IdCategoria == null || !await _categoryRepository.ExistsById(productDTO.IdCategoria.Value))
                    return Result<bool>.Failure("categoria_invalida");

                if (productDTO.IdUbicacion == null || !await _productRepository.ExisteUbicacion(productDTO.IdUbicacion.Value))
                    return Result<bool>.Failure("ubicacion_invalida");

                foreach (var cb in productDTO.CodigoBarras)
                {
                    if (await _productRepository.CodigoBarraExists(cb) && !existingProduct.CodigoBarras.Any(e => e.Codigo == cb.Codigo))
                        return Result<bool>.Failure($"codigo_barra_duplicado: {cb.Codigo}");
                }

                // Mapear los cambios al producto existente
                _mapper.Map(productDTO, existingProduct);

                await _productRepository.Update(existingProduct);

                return Result<bool>.Succes();
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure("error_inesperado");
            }
        }

        public async Task<Result<List<ProductDTO>>> GetAll()
        {
            try
            {
                var products = await _productRepository.GetAll();
                var dtos = _mapper.Map<List<ProductDTO>>(products);
                return Result<List<ProductDTO>>.Succes(dtos);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<List<ProductDTO>>.Failure("error_inesperado");
            }
        }

        public async Task<Result<List<ProductDetailDTO>>> GetAllWithCategoryAndLocation()
        {
            try
            {
                var products = await _productRepository.GetAllWithCategoryAndLocation();
                var dtos = _mapper.Map<List<ProductDetailDTO>>(products);
                return Result<List<ProductDetailDTO>>.Succes(dtos);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<List<ProductDetailDTO>>.Failure("error_inesperado");
            }
        }

        public async Task<Result<ProductDTO>> GetById(int idProducto)
        {
            try
            {
                var product = await _productRepository.GetById(idProducto);
                if (product == null) return Result<ProductDTO>.Failure("product_not_found");
                var dto = _mapper.Map<ProductDTO>(product);
                return Result<ProductDTO>.Succes(dto);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<ProductDTO>.Failure("error_inesperado");
            }
        }
    }
}