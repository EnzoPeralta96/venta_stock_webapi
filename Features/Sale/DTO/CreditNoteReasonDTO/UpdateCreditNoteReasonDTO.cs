using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Sale.DTO.CreditNoteReasonDTO;

public class UpdateCreditNoteReasonDTO
{
    [Required(ErrorMessage = "El ID del motivo es obligatorio.")]
    public int IdMotivo { get; set; }

    [Required(ErrorMessage = "El nombre del motivo es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = null!;
}
