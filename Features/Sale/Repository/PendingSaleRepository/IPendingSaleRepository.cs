using proyecto_venta_stock.Models;
using venta_stock_webapi.Sale.DTO;

namespace venta_stock_webapi.Sale.Repository
{
    public interface IPendingSaleRepository
    {
        /// <summary>
        /// Crear una venta pendiente de autorización
        /// </summary>
        Task<VentaPendiente> CreatePendingSaleAsync(VentaPendiente ventaPendiente);

        /// <summary>
        /// Obtener venta pendiente por ID
        /// </summary>
        Task<VentaPendiente?> GetByIdAsync(int idVentaPendiente);

        /// <summary>
        /// Listar ventas pendientes con paginación
        /// </summary>
        Task<List<PendingSaleListDTO>> GetPendingSalesAsync(int? idEstado = null);

        /// <summary>
        /// Actualizar estado de venta pendiente
        /// </summary>
        Task UpdateAsync(VentaPendiente ventaPendiente);
    }
}