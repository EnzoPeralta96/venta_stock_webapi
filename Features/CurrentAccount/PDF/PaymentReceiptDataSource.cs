using proyecto_venta_stock.Models;

namespace venta_stock_webapi.CurrentAccount.PDF
{
    public class PaymentReceiptDataSource
    {
        public Ferreteria DatosFerreteria { get; set; } = new();

        public DateTime Fecha { get; set; }

        // Cliente
        public string ClienteNombre { get; set; }
        public string ClienteDni { get; set; }
        public string ClienteTelefono { get; set; }

        // Comprobante — título y color del header según tipo de movimiento
        // TipoComprobante: "COMPROBANTE DE PAGO" | "NOTA DE DÉBITO" | "NOTA DE CRÉDITO" | "ANULACIÓN DE PAGO"
        // ColorHeader: "green" | "red" | "blue"
        public string TipoComprobante { get; set; }
        public string ColorHeader { get; set; }

        public string Detalle { get; set; }
        public decimal Importe { get; set; }
        public string ImporteEnLetras { get; set; }
        public decimal SaldoResultante { get; set; }
        public string UsuarioRegistra { get; set; }

        // Venta asociada (solo para pago_factura)
        public string CodigoVenta { get; set; }
        public decimal? TotalVenta { get; set; }

        // Motivo de nota de débito (solo para tipo NOTA_DEBITO = 3)
        public string? MotivoNd { get; set; }

        // Motivo de nota de crédito (solo para tipo NOTA_CREDITO = 4)
        public string? MotivoNc { get; set; }
    }
}
