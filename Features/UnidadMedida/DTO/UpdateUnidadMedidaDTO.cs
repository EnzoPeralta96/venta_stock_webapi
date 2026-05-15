using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Features.UnidadMedida.DTO;

public class UpdateUnidadMedidaDTO
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(50, ErrorMessage = "El nombre no puede superar los 50 caracteres.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "La abreviatura es obligatoria.")]
    [MaxLength(10, ErrorMessage = "La abreviatura no puede superar los 10 caracteres.")]
    public string Abreviatura { get; set; } = null!;
}
