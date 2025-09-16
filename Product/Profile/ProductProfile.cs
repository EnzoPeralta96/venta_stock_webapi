namespace proyecto_venta_stock.Product.Profile
{
    using AutoMapper;
    using proyecto_venta_stock.Models;
    using proyecto_venta_stock.Product.DTO;

    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<ProductDTO, Producto>();
        }
    }
}