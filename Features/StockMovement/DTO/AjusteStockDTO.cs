using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Features.StockMovement.DTO;

public class AjusteStockDTO
{
    [Required(ErrorMessage = "El IdProducto es obligatorio.")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    public decimal Cantidad { get; set; } // positivo suma, negativo resta

    [Required(ErrorMessage = "El IdTipoMovimiento es obligatorio.")]
    [Range(5, 7, ErrorMessage = "El IdTipoMovimiento debe ser 5, 6 o 7.")]
    public int IdTipoMovimiento { get; set; } // 5, 6 o 7

    [Required(ErrorMessage = "El motivo es obligatorio.")]
    public string Motivo { get; set; } = string.Empty;
}
