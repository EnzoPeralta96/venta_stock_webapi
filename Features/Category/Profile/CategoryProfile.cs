using AutoMapper;
using proyecto_venta_stock.Category.DTO;
using proyecto_venta_stock.Models;
using venta_stock_webapi.Features.Category.DTO;

namespace proyecto_venta_stock.Category.Profile
{
    public class CategoryProfile : AutoMapper.Profile
    {
        /// <summary>
        /// ReverseMap() se utiliza para configurar mapeos bidireccionales entre los DTOs y la entidad Categorium, 
        /// lo que permite convertir fácilmente entre ambos tipos de objetos en ambas direcciones.
        /// Configura los mapeos de AutoMapper para la entidad Categorium y sus DTOs relacionados.
        /// </summary>
        /// <remarks>
        /// Este constructor establece las siguientes asignaciones:
        /// - Mapea una entidad Categorium a CategoryDetailDTO para obtener los detalles de una categoría.
        /// - Mapea CreateCategoryDTO a Categorium y viceversa para crear nuevas categorías.
        /// - Mapea UpdateCategoryDTO a Categorium y viceversa para actualizar categorías existentes.
        /// </remarks>
        public CategoryProfile()
        {
            CreateMap<Categorium, CategoryDetailDTO>().ReverseMap();
            CreateMap<CreateCategoryDTO, Categorium>().ReverseMap();
            CreateMap<UpdateCategoryDTO, Categorium>().ReverseMap();
        }
    }
}