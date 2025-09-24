using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Location.DTO;


namespace proyecto_venta_stock.Location.Services
{
    public interface ILocationServices
    {
        Task<Result<bool>> Create(LocationDTO locationDTO);
        Task<Result<bool>> Update(LocationDTO locationDTO);
        Task<Result<List<LocationDTO>>> GetAll();
        Task<Result<LocationDTO>> GetById(int idUbicacion);
        Task<Result<bool>> Delete(int idUbicacion);
    }
}