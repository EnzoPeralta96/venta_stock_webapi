using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using venta_stock_webapi.Sale.DTO;

namespace venta_stock_webapi.Sale.Strategies
{
    
    /// Define el contrato para las estrategias de procesamiento de ventas
    /// Cada tipo de venta (efectivo, cuenta corriente, tarjeta, etc.) 
    /// implementa esta interface con su lógica específica.
    /// La lógica COMÚN (validaciones, guardar venta, detalles, stock) 
    /// está en SaleService, NO en las estrategias.
    public interface ISaleStrategy
    {
        /// <summary>
        /// Procesa la lógica específica del tipo de venta
        /// </summary>
        /// <param name="saleDTO">Datos de la venta desde el cliente</param>
        /// <param name="venta">Entidad Venta preparada (con código, total, etc.)</param>
        /// <param name="detalles">Lista de detalles de venta preparados</param>
        /// <returns>
        /// Result con la venta si el procesamiento fue exitoso,
        /// o un código de error si falló alguna validación específica
        /// </returns>
        Task<Result<Ventum>> ProcessSaleAsync(
            CreateSaleDTO saleDTO,
            Ventum venta,
            List<DetalleVentum> detalles
        );
    }
}