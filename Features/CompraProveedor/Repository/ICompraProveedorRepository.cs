using proyecto_venta_stock.Models;
using venta_stock_webapi.Shared.Paged;

namespace proyecto_venta_stock.CompraProveedor.Repository;

public interface ICompraProveedorRepository
{
    Task<Models.CompraProveedor> CreateAsync(Models.CompraProveedor compra);
    Task<List<Models.CompraProveedor>> GetAllWithDetailsAsync(DateOnly? fechaDesde, DateOnly? fechaHasta);
    Task<PagedList<Models.CompraProveedor>> GetPagedWithDetailsAsync(int pageIndex, int pageSize, string? search, bool? activo, DateOnly? fechaDesde, DateOnly? fechaHasta);
    Task<PagedList<Models.CompraProveedor>> GetPagedByProveedorAsync(int idProveedor, int pageIndex, int pageSize, bool? activo, DateOnly? fechaDesde, DateOnly? fechaHasta);
    Task<Models.CompraProveedor?> GetByIdAsync(int idCompraProveedor);
    Task<Models.CompraProveedor?> GetByIdWithDetailsAsync(int idCompraProveedor);
    Task<List<Models.CompraProveedor>> GetAllByProveedorWithDetailsAsync(int idProveedor, DateOnly? fechaDesde, DateOnly? fechaHasta);
    Task<bool> ExistsByNumeroComprobanteAsync(string numeroComprobante, int idProveedor, int? excludeId = null);
}
