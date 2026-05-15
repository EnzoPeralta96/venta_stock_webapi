using System;

namespace venta_stock_webapi.CurrentAccount.DTO.MovementDTO
{
    public class PendingSalePaymentDTO
    {
        public int IdVenta { get; set; }
        public string CodigoVenta { get; set; }
        public DateTime? Fecha { get; set; }
        public decimal TotalVenta { get; set; }
        public decimal TotalPagado { get; set; }
        public decimal SaldoPendiente { get; set; }
    }
}
