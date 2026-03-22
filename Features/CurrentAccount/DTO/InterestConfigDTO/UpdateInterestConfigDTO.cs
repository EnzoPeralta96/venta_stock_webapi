using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.CurrentAccount.DTO.InterestConfigDTO;

public class UpdateInterestConfigDTO
{
    [Required]
    public int IdConfig { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100)]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El porcentaje de interés es obligatorio.")]
    [Range(0.01, 100, ErrorMessage = "El porcentaje debe estar entre 0.01 y 100.")]
    public decimal PorcentajeInteres { get; set; }

    [Required(ErrorMessage = "El día de vencimiento es obligatorio.")]
    [Range(1, 28, ErrorMessage = "El día de vencimiento debe estar entre 1 y 28.")]
    public int DiaVencimiento { get; set; }
}
