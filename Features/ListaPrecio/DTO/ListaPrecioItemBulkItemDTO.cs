using System.ComponentModel.DataAnnotations;

namespace proyecto_venta_stock.ListaPrecio.DTO;

public class ListaPrecioItemBulkItemDTO
{
    [Required(ErrorMessage = "El producto es requerido.")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El precio es requerido.")]
    [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser mayor o igual a 0.")]
    public decimal Precio { get; set; } // costo neto sin IVA
}
