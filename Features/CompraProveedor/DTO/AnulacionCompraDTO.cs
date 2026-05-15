using System.ComponentModel.DataAnnotations;

namespace proyecto_venta_stock.CompraProveedor.DTO;

public class AnulacionCompraDTO
{
    [Required(ErrorMessage = "El motivo de anulación es obligatorio.")]
    public string Motivo { get; set; } = null!;
}
