using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Features.StockMovement.Repository;

public interface IMovimientoStockRepository
{
    void Add(MovimientoStock movimientoStock);
    Task SaveChangesAsync();

    // Movimientos
    IQueryable<MovimientoStock> MovementsQueryable(int idProducto, int? idTipoMovimiento);

    // Tipos de movimiento
    Task<List<TipoMovimientoStock>> GetTiposAsync();
    Task<List<TipoMovimientoStock>> GetTiposAdminAsync();
    Task<TipoMovimientoStock?> GetTipoByIdAsync(int id);
    Task<bool> NombreEnUsoAsync(string nombre, int? excludeId = null);
    void AddTipo(TipoMovimientoStock tipo);
}
