using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Features.StockMovement.Repository;

public interface IMovimientoStockRepository
{
    void Add(MovimientoStock movimientoStock);
    Task SaveChangesAsync();

    Task<List<TipoMovimientoStock>> GetTiposAsync();
    IQueryable<MovimientoStock> MovementsQueryable(int idProducto, int? idTipoMovimiento);
}
