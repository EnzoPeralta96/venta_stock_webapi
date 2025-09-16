using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Product.DTO;
using proyecto_venta_stock.Product.ProductRepository;

namespace proyecto_venta_stock.Product.Services
{
    public class ProductServices : IProductServices
    {
        private readonly ILogger<ProductServices> _logger;
        private readonly IProductRepository _productRepository;
        private readonly IMapper _mapper;
        public ProductServices(IProductRepository productRepository, ILogger<ProductServices> logger, IMapper mapper)
        {
            _productRepository = productRepository;
            _logger = logger;
            _mapper = mapper;
        }
        public async Task<Result<bool>> Create(ProductDTO productDTO)
        {
            try
            {
                bool productExists = await _productRepository.Exists(productDTO.Nombre, productDTO.Marca);


                if (productExists) return Result<bool>.Failure("product_name_in_use");

                if (productDTO.IdCategoria == null || !await _productRepository.ExisteCategoria(productDTO.IdCategoria.Value))
                    return Result<bool>.Failure("categoria_invalida");

                if (productDTO.IdUbicacion == null || !await _productRepository.ExisteUbicacion(productDTO.IdUbicacion.Value))
                    return Result<bool>.Failure("ubicacion_invalida");

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

    }
}