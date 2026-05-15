using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Features.Ferreteria.DTO;

namespace venta_stock_webapi.Features.Ferreteria.Services
{
    public interface IFerreteriaService
    {
        Task<Result<FerreteriaResponseDTO>> GetAsync();
        Task<Result<bool>> UpdateAsync(FerreteriaUpdateDTO ferreteria);
    }
}
