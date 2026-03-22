using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.CompraProveedor.Repository;

public interface ICompraProveedorRepository
{
    Task<Models.CompraProveedor> CreateAsync(Models.CompraProveedor compra);
    Task UpdateAsync(Models.CompraProveedor compra);
    Task<List<Models.CompraProveedor>> GetAllAsync();
    Task<List<Models.CompraProveedor>> GetAllWithDetailsAsync();
    Task<Models.CompraProveedor?> GetByIdAsync(int idCompraProveedor);
    Task<Models.CompraProveedor?> GetByIdWithDetailsAsync(int idCompraProveedor);
    Task<List<Models.CompraProveedor>> GetByProveedorAsync(int idProveedor);
    Task<bool> ExistsByNumeroComprobanteAsync(string numeroComprobante, int idProveedor, int? excludeId = null);
    Task ToggleEstadoAsync(int idCompraProveedor, bool activo);
}
