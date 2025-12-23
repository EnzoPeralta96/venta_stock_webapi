namespace venta_stock_webapi.Sale.Services
{
    public interface IPdfService
    {
        /// <summary>
        /// Genera PDF de una venta
        /// </summary>
        Task<byte[]> GenerateSalePdfAsync(int idVenta);
        
        /// <summary>
        /// Genera PDF de una venta pendiente
        /// </summary>
        Task<byte[]> GeneratePendingSalePdfAsync(int idVentaPendiente);
    }
}