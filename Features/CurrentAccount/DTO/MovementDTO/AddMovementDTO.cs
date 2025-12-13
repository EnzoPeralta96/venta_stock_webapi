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
        [Required (ErrorMessage = "El IdVenta es obligatorio.")]
        
        public int IdVenta { get; set; }

        [Required (ErrorMessage = "El IdUsuarioRegistra es obligatorio.")]
        public int IdUsuarioRegistra { get; set; } = 1;

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Importe <= 0)
            {
                yield return new ValidationResult(
                    "El Importe debe ser mayor que 0.",
                    new[] { nameof(Importe) }
                );
            }

            if (IdTipoMovimiento ==  8)
            {
                yield return new ValidationResult(
                    "Para el pago de una factura, el IdVenta es requerido",
                    new[] { nameof(IdTipoMovimiento) }
                );    
            }
        }
    }



}