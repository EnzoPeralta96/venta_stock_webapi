using System.ComponentModel.DataAnnotations;

namespace proyecto_venta_stock.ListaPrecio.DTO;

public class ListaPrecioItemUpsertDTO
{
    [Required(ErrorMessage = "El producto es requerido.")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El precio es requerido.")]
    public decimal Precio { get; set; }

    public decimal? Margen { get; set; }
}
