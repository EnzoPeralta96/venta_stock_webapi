using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Sale.DTO;

namespace venta_stock_webapi.Sale.Strategies
{
    /// <summary>
    /// Estrategia para procesar ventas en EFECTIVO
    /// </summary>
    /// <remarks>
    /// Lógica específica:
    /// - NO requiere validaciones adicionales (el pago es inmediato)
    /// - NO crea movimientos en cuenta corriente
    /// 
    /// La lógica común (guardar venta, detalles, stock) está en SaleService
    /// </remarks>
    public class CashSaleStrategy : ISaleStrategy
    {
        private readonly ILogger<CashSaleStrategy> _logger;

        public CashSaleStrategy(ILogger<CashSaleStrategy> logger)
        {
            _logger = logger;
        }

        public async Task<Result<Ventum>> ProcessSaleAsync(
            CreateSaleDTO saleDTO,
            Ventum venta,
            List<DetalleVentum> detalles)
        {
            _logger.LogInformation("Procesando venta en EFECTIVO: {codigo}", venta.CodigoVenta);

            // Para ventas en efectivo NO hay lógica especial
            // Todo se maneja en SaleService (guardar venta, detalles, stock)
            
            // Simplemente retornamos éxito
            // El service se encarga del resto

            _logger.LogInformation("Venta en efectivo validada OK: {codigo}", venta.CodigoVenta);

            return await Task.FromResult(Result<Ventum>.Success(venta));
        }
    }
}