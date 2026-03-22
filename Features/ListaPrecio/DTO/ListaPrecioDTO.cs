using System.ComponentModel.DataAnnotations;

namespace proyecto_venta_stock.ListaPrecio.DTO;

public class ListaPrecioDTO
{
    public int IdLista { get; set; }
    public int? IdProveedor { get; set; }
    public string? Nombre { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; }
    public int? IdUsuarioRegistra { get; set; }
    public int CantidadItems { get; set; }
}

public class ListaPrecioCreateDTO
{
    [Required(ErrorMessage = "El proveedor es requerido.")]
    public int IdProveedor { get; set; }

    [Required(ErrorMessage = "El nombre de la lista es requerido.")]
    public string Nombre { get; set; }

    public string? Observaciones { get; set; }
}

public class ListaPrecioUpdateDTO
{
    [Required(ErrorMessage = "El identificador de la lista es requerido.")]
    public int IdLista { get; set; }

    [Required(ErrorMessage = "El nombre de la lista es requerido.")]
    public string Nombre { get; set; }

    public string? Observaciones { get; set; }
}

public class ListaPrecioItemDTO
{
    public int IdLista { get; set; }
    public int IdProducto { get; set; }
    public decimal Precio { get; set; }
    public decimal? Margen { get; set; }
    public string? NombreProducto { get; set; }
    public string? Marca { get; set; }
}

public class ListaPrecioItemUpsertDTO
{
    [Required(ErrorMessage = "El producto es requerido.")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El precio es requerido.")]
    public decimal Precio { get; set; }

    public decimal? Margen { get; set; }
}
