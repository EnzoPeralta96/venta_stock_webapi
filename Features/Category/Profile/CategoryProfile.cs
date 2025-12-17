using AutoMapper;
using proyecto_venta_stock.Category.DTO;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.Category.Profile
{
    public class CategoryProfile : AutoMapper.Profile
    {
        public CategoryProfile()
        {
            CreateMap<Categorium, CategoryBasicDTO>().ReverseMap();
            CreateMap<Categorium, CategoryDetailDTO>().ReverseMap();
        }
    }
}