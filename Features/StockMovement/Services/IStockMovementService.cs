using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Features.StockMovement.DTO;
using venta_stock_webapi.Shared.Paged;

namespace venta_stock_webapi.Features.StockMovement.Services;

public interface IStockMovementService
{
    Task<Result<bool>> RegistrarMovimientoAsync(
        int idProducto,
        TipoMovimientoStockEnum tipoMovimiento,
        decimal cantidad,
        string? referencia,
        int idUsuario);

    Task<Result<List<TipoMovimientoStockDTO>>> GetTiposMovimientoAsync();

    Task<Result<PagedList<MovimientoStockDTO>>> MovimientosPagedAsync(
        int idProducto,
        int pageIndex,
        int pageSize,
        int? idTipoMovimiento);
}
