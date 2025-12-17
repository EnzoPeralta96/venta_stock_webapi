using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.Location.DTO;
using venta_stock_webapi.Shared.Paged;


namespace proyecto_venta_stock.Location.Services
{

    public interface ILocationService
    {
        Task<Result<PagedList<LocationDTO>>> SearchAsync(int pageIndex, int pageSize, string searchTerm, bool activos);
        Task<Result<LocationDTO>> GetByIdAsync(int id);
        Task<Result<LocationDTO>> CreateAsync(LocationCreateUpdateDTO dto);
        Task<Result<LocationDTO>> UpdateAsync(int id, LocationCreateUpdateDTO dto);
        Task<Result<bool>> DeleteAsync(int id);
        Task<Result<bool>> ToggleActivoAsync(int id);
    }


}