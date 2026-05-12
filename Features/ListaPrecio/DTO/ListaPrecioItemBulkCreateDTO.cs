using System.ComponentModel.DataAnnotations;

namespace proyecto_venta_stock.ListaPrecio.DTO;

public class ListaPrecioItemBulkCreateDTO
{
    [Required]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un producto.")]
    public List<ListaPrecioItemBulkItemDTO> Items { get; set; } = new();
}
