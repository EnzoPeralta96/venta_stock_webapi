using System.ComponentModel.DataAnnotations;

namespace proyecto_venta_stock.ListaPrecio.DTO;

public class ListaPrecioUpdateDTO
{
    [Required(ErrorMessage = "El identificador de la lista es requerido.")]
    [Range(1, int.MaxValue, ErrorMessage = "El identificador de la lista es requerido.")]
    public int IdLista { get; set; }

    [Required(ErrorMessage = "El nombre de la lista es requerido.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 200 caracteres.")]
    public string Nombre { get; set; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
    public string? Observaciones { get; set; }

    [Range(0, 100, ErrorMessage = "El IVA por defecto debe estar entre 0 y 100.")]
    public decimal IvaPorDefecto { get; set; } = 21;
}
