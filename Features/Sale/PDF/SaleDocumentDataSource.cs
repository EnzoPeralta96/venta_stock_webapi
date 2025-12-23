
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Sale.PDF
{
    public class SaleDocumentDataSource
    {
        // Info de la empresa
        public Ferreteria DatosFerreteria = new();

        // Info de la venta
        public string CodigoVenta { get; set; }
        public DateTime Fecha { get; set; }
        public string TipoComprobante { get; set; } = "COMPROBANTE DE VENTA";
        
        // Cliente
        public string ClienteNombre { get; set; }
        public string ClienteDni { get; set; }
        public string ClienteTelefono { get; set; }
        
        // Vendedor
        public string VendedorNombre { get; set; }
        
        // Medio de pago
        public string MedioPago { get; set; }
        
        // Items
        public List<SaleItemPdf> Items { get; set; } = new();
        
        // Totales
        public decimal Subtotal { get; set; }
        public decimal Descuento { get; set; }
        public decimal Total { get; set; }
        public string TotalEnTexto { get; set; }

        // Info adicional
        public string Observaciones { get; set; }
        public bool EsVentaPendiente { get; set; }
        public decimal? Excedente { get; set; }
    }

    public class SaleItemPdf
    {
        public int Numero { get; set; }
        public string Producto { get; set; }
        public string Marca { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}