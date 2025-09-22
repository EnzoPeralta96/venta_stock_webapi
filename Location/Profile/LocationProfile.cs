using AutoMapper;
using System.Linq;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Location.DTO;

namespace proyecto_venta_stock.Location.Profile
{
    public class LocationProfile : AutoMapper.Profile
    {
        public LocationProfile()
        {
             CreateMap<LocationDTO, Ubicacion>()
                .ForMember(d => d.Seccion, o => o.MapFrom(s => (s.Seccion ?? string.Empty).Trim().ToUpper()))
                .ReverseMap()
                // Al volver al DTO, no forzamos mayúsculas; solo evitamos nulls
                .ForMember(d => d.Seccion, o => o.MapFrom(s => s.Seccion ?? string.Empty));
        }
    }
}