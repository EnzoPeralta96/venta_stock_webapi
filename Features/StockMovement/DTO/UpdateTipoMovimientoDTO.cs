using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Features.StockMovement.DTO;

public class UpdateTipoMovimientoDTO
{
    [Required]
    [MaxLength(50)]
    public string Nombre { get; set; } = null!;

    [MaxLength(255)]
    public string? Descripcion { get; set; }
}