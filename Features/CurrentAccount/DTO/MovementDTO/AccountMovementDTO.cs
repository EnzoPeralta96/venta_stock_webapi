using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace venta_stock_webapi.CurrentAccount.DTO.MovementDTO
{
    public class AccountMovementDTO
    {
        public int IdMovimiento { get; set; }
        public string TipoMovimiento {get; set;}
        public string Detalle { get; set; }
        public string Estado {get; set;}

        //Fecha que se produjo el moviento
        public DateTime Fecha { get; set; }
        public decimal Importe { get; set; }
        public decimal? SaldoActual { get; set; }
        public decimal? LimiteCuenta { get; set; }
        //Para el caso del usuario que da de alta al cliente
        public string UsuarioRegistra {get; set;}

        public int? IdVenta { get; set; }
        public int? IdCliente { get; set; }

        // Información de la venta asociada (cuando IdVenta != null)
        public string CodigoVenta { get; set; }
        public DateTime? FechaVenta { get; set; }
        public decimal? TotalVenta { get; set; }
        public string EstadoVenta { get; set; }

        // Estado de pago del consumo — solo relevante para tipo movimiento_cc
        // Derivado de MontoPagado vs Importe: "pendiente" | "parcial" | "pagado" | null
        public decimal? MontoPagado { get; set; }
        public string EstadoPago { get; set; }

        // Indica si este pago fue anulado (solo relevante para pago_global y pago_factura)
        public bool EsAnulado { get; set; }

        // Motivo de nota de débito (solo relevante para tipo NOTA_DEBITO = 3)
        public int? IdMotivoNd { get; set; }
        public string? MotivoNd { get; set; }

        // Motivo de nota de crédito (solo relevante para tipo NOTA_CREDITO = 4)
        public int? IdMotivoNc { get; set; }
        public string? MotivoNc { get; set; }
    }
}