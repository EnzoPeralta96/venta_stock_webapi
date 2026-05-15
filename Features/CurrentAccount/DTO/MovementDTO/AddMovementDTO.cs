using System.ComponentModel.DataAnnotations;


namespace venta_stock_webapi.CurrentAccount.DTO.MovementDTO
{
    public class AddMovementDTO : IValidatableObject
    {
        [Required (ErrorMessage = "El IdCliente es obligatorio.")]
        public int IdCliente { get; set; }

        [Required (ErrorMessage = "El Importe es obligatorio.")]
        public decimal Importe { get; set; }

        [Required (ErrorMessage = "El Detalle es obligatorio.")]
        public string Detalle {get; set;}

        [Required (ErrorMessage = "El IdTipoMovimiento es obligatorio.")]
        public int IdTipoMovimiento { get; set; }

        public int? IdVenta { get; set; }

        [Required (ErrorMessage = "El IdUsuarioRegistra es obligatorio.")]
        public int IdUsuarioRegistra { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Importe <= 0)
            {
                yield return new ValidationResult(
                    "El Importe debe ser mayor que 0.",
                    new[] { nameof(Importe) }
                );
            }

            if ((IdTipoMovimiento == 5 || IdTipoMovimiento == 8)&& IdVenta is null)
            {
                yield return new ValidationResult(
                    "Para el pago de una factura, el IdVenta es requerido",
                    new[] { nameof(IdTipoMovimiento) }
                );    
            }

            if (IdTipoMovimiento == 3)
            {
                yield return new ValidationResult(
                    "La Nota de Débito debe registrarse mediante el endpoint dedicado /register-debit-note.",
                    new[] { nameof(IdTipoMovimiento) }
                );
            }

            if (IdTipoMovimiento == 7)
            {
                yield return new ValidationResult(
                    "El tipo Interés Saldo Global está deprecado. Use Nota de Débito con motivo 'Interés por mora'.",
                    new[] { nameof(IdTipoMovimiento) }
                );
            }
        }
    }



}