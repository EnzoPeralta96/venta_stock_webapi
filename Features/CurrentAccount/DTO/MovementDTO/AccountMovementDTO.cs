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
        public string UsuarioRegistra {get; set;}

        //Fecha que autorizan el movimiento.
        public DateTime? FechaAutorizacion { get; set; }

        //Para el caso que el movimiento necesite autorizacion
        public string UsuarioAutoriza {get; set;}

        //Para el caso del usuario que da de alta al cliente
        public int? IdVenta { get; set; }
        public int? IdCliente { get; set; }    
    }
}