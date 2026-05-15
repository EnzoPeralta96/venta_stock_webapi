using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Features.UnidadMedida.DTO;

namespace venta_stock_webapi.Features.UnidadMedida.Services;

public interface IUnidadMedidaService
{
    Task<Result<List<UnidadMedidaDTO>>> GetAllAsync();
    Task<Result<List<UnidadMedidaDTO>>> GetActivosAsync();
    Task<Result<UnidadMedidaDTO>> CreateAsync(CreateUnidadMedidaDTO dto);
    Task<Result<UnidadMedidaDTO>> UpdateAsync(int id, UpdateUnidadMedidaDTO dto);
    Task<Result<bool>> ToggleActivoAsync(int id);
}
