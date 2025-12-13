using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace venta_stock_webapi.CurrentAccount.DTO.MovementDTO
{
    public class TypeMovementDTO
    {
        public int IdTipoMovimiento { get; set; }

        public string Nombre { get; set; }

        public string Accion { get; set; }
    }
}