using System.ComponentModel.DataAnnotations;

namespace proyecto_venta_stock.Product.DTO;

public class ActualizarMasivoManualDTO
{
    [Required, MinLength(1)]
    public List<ActualizarPrecioManualItemDTO> Items { get; set; } = new();
}

public class ActualizarPrecioManualItemDTO
{
    [Required] public int IdProducto { get; set; }
    [Range(0, double.MaxValue)] public decimal CostoNeto { get; set; }
    [Range(0, 100)] public decimal IvaPorcentaje { get; set; } = 21;
    public decimal? Margen { get; set; }
}


