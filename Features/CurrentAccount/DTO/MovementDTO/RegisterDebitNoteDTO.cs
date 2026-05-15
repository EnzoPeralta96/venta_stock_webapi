using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.CurrentAccount.DTO.MovementDTO;

public class RegisterDebitNoteDTO : IValidatableObject
{
    [Required(ErrorMessage = "El ID del cliente es obligatorio.")]
    public int IdCliente { get; set; }

    [Required(ErrorMessage = "El importe es obligatorio.")]
    public decimal Importe { get; set; }

    [Required(ErrorMessage = "El ID del motivo es obligatorio.")]
    public int IdMotivo { get; set; }

    /// <summary>
    /// Opcional. Si se especifica, la ND está vinculada a una venta
    /// (caso "Ajuste de precio").
    /// </summary>
    public int? IdVenta { get; set; }

    /// <summary>
    /// Detalle adicional opcional ingresado por el operario.
    /// </summary>
    public string? DetalleAdicional { get; set; }

    [Required(ErrorMessage = "El ID del usuario que registra es obligatorio.")]
    public int IdUsuarioRegistra { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Importe <= 0)
        {
            yield return new ValidationResult(
                "El importe debe ser mayor que 0.",
                new[] { nameof(Importe) });
        }
    }
}
