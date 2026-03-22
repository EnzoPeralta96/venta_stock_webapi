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

        /// <summary>
        /// Genera el PDF de Nota de Crédito para una venta anulada.
        /// Aplica tanto a ventas al contado como a ventas CC.
        /// </summary>
        Task<byte[]> GenerateAnnulReceiptAsync(int idVenta);
    }
}