using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Sale.DTO;

namespace venta_stock_webapi.Sale.Services
{
    public interface IPendingSaleService
    {
        /// <summary>
        /// Registrar una venta como pendiente de autorización
        /// </summary>
        Task<Result<PendingSaleResponseDTO>> CreatePendingSaleAsync(
            CreateSaleDTO saleDTO,
            decimal saldoActual,
            decimal limiteCuenta
        );

        /// <summary>
        /// Obtener detalle de una venta pendiente
        /// </summary>
        Task<Result<PendingSaleResponseDTO>> GetPendingSaleByIdAsync(int idVentaPendiente);

        /// <summary>
        /// Listar ventas pendientes
        /// </summary>
        Task<Result<List<PendingSaleListDTO>>> GetPendingSalesAsync(int? idEstado = null);

        /// <summary>
        /// Autorizar o rechazar una venta pendiente
        /// </summary>
        Task<Result<AuthorizeSaleResponseDTO>> AuthorizeSaleAsync(
            AuthorizeSaleDTO dto,
            int idUsuarioAutoriza
        );
    }
}