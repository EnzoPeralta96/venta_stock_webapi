using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Sale.DTO;

public class AnnulSaleDTO
{
    [Required(ErrorMessage = "El ID del motivo es obligatorio.")]
    public int IdMotivo { get; set; }

    /// <summary>Detalle adicional opcional.</summary>
    public string? DetalleAdicional { get; set; }
}
