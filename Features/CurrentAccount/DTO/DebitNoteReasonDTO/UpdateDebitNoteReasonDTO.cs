using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.CurrentAccount.DTO.DebitNoteReasonDTO;

public class UpdateDebitNoteReasonDTO
{
    [Required(ErrorMessage = "El ID del motivo es obligatorio.")]
    public int IdMotivo { get; set; }

    [Required(ErrorMessage = "El nombre del motivo es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "La categoría del motivo es obligatoria.")]
    [RegularExpression("^(general|ajuste_precio|interes_mora)$",
        ErrorMessage = "La categoría debe ser 'general', 'ajuste_precio' o 'interes_mora'.")]
    public string Categoria { get; set; } = null!;
}
