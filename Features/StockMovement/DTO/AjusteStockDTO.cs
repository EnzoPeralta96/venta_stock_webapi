using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Features.StockMovement.DTO;

public class AjusteStockDTO
{
    [Required(ErrorMessage = "El IdProducto es obligatorio.")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "La cantidad es obligatoria.")]
    [Range(0.001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a 0.")]
    public decimal Cantidad { get; set; }  // siempre positiva — el backend aplica el signo según EsPositivo del tipo

    [Required(ErrorMessage = "El IdTipoMovimiento es obligatorio.")]
    public int IdTipoMovimiento { get; set; }  // cualquier tipo activo no-sistema

    [Required(ErrorMessage = "El motivo es obligatorio.")]
    public string Motivo { get; set; } = string.Empty;
}
