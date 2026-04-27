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
    public decimal IvaPorDefecto { get; set; }
}

public class ListaPrecioCreateDTO
{
    [Required(ErrorMessage = "El proveedor es requerido.")]
    [Range(1, int.MaxValue, ErrorMessage = "El proveedor es requerido.")]
    public int IdProveedor { get; set; }

    [Required(ErrorMessage = "El nombre de la lista es requerido.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "El nombre debe tener entre 2 y 200 caracteres.")]
    public string Nombre { get; set; }

    [StringLength(500, ErrorMessage = "Las observaciones no pueden superar los 500 caracteres.")]
    public string? Observaciones { get; set; }

    [Range(0, 100, ErrorMessage = "El IVA por defecto debe estar entre 0 y 100.")]
    public decimal IvaPorDefecto { get; set; } = 21;
}

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

public class ListaPrecioItemDTO
{
    public int IdLista { get; set; }
    public int IdProducto { get; set; }
    public decimal Precio { get; set; }
    public decimal? Margen { get; set; }
    public string? NombreProducto { get; set; }
    public string? Marca { get; set; }
    public int? IdUnidadMedida { get; set; }
}

public class ListaPrecioItemUpsertDTO
{
    [Required(ErrorMessage = "El producto es requerido.")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El precio es requerido.")]
    public decimal Precio { get; set; }

    public decimal? Margen { get; set; }
}

public class ListaPrecioItemBulkItemDTO
{
    [Required(ErrorMessage = "El producto es requerido.")]
    public int IdProducto { get; set; }

    [Required(ErrorMessage = "El precio es requerido.")]
    [Range(0, double.MaxValue, ErrorMessage = "El precio debe ser mayor o igual a 0.")]
    public decimal Precio { get; set; } // costo neto sin IVA
}

public class ListaPrecioItemBulkCreateDTO
{
    [Required]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un producto.")]
    public List<ListaPrecioItemBulkItemDTO> Items { get; set; } = new();
}

public class BulkAddResultDTO
{
    public int Insertados { get; set; }
    public int Actualizados { get; set; }
    public List<int> Ignorados { get; set; } = new();
}
