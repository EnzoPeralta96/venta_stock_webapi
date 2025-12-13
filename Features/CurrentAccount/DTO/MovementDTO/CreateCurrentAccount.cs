using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace venta_stock_webapi.CurrentAccount.DTO.MovementDTO
{
    public class CreateCurrentAccountDTO : IValidatableObject
    {
        [Required(ErrorMessage = "El Detalle es obligatorio")]
        public string Detalle { get; set; }

        [Required(ErrorMessage = "El Importe es obligatorio")]
        public decimal LimiteCuenta { get; set; }

        [Required(ErrorMessage = "El Id del cliente es obligatorio")]
        public int IdCliente { get; set; }

        [Required(ErrorMessage = "El Id del usuario que registra es obligatorio")]
        public int IdUsuarioRegistra { get; set; }
        public bool TieneDueda { get; set; }
        public decimal SaldoActual { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            // ---------------------------
            // Si tiene deuda, el saldo actual debe ser mayor que 0
            // ---------------------------
            if (TieneDueda)
            {
                if (SaldoActual <= 0)
                {
                    yield return new ValidationResult(
                        "El saldo actual debe ser mayor que 0 si el cliente tiene deuda.",
                        new[] { nameof(SaldoActual) }
                    );
                }
            }
        }





    }
}