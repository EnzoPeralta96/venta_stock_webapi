namespace proyecto_venta_stock.Product.Profile
{
    using AutoMapper;
    using System.Linq;
    using proyecto_venta_stock.Models;
    using proyecto_venta_stock.Product.DTO;

    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<ProductDTO, Producto>()
             .ForMember(dest => dest.CodigoBarras,
                 opt => opt.MapFrom(src => src.CodigoBarras.Select(cb => new CodigoBarra { Codigo = cb.Codigo }).ToList()));

            CreateMap<CodigoBarraDTO, CodigoBarra>();

            // Reverse mappings for read operations
            CreateMap<Producto, ProductDTO>();
            CreateMap<CodigoBarra, CodigoBarraDTO>();

            CreateMap<Producto, ProductDetailDTO>()
                .ForMember(dest => dest.Categoria,
                    opt => opt.MapFrom(src => src.IdCategoriaNavigation != null ? src.IdCategoriaNavigation.Categoria : "Sin Categoria"))
                .ForMember(dest => dest.Ubicacion,
                    opt => opt.MapFrom(src => src.IdUbicacionNavigation != null ? (src.IdUbicacionNavigation.Fila + " " + src.IdUbicacionNavigation.Seccion + " " + src.IdUbicacionNavigation.Nivel) : "Sin Ubicación"))
                .ForMember(dest => dest.CodigoBarras,
                 opt => opt.MapFrom(src => src.CodigoBarras.Select(cb => new CodigoBarra { Codigo = cb.Codigo }).ToList()));
        }
        
        
    }
}