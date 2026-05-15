namespace proyecto_venta_stock.Models;
public partial class ListaPrecio
{
    public int IdLista { get; set; }

    public int? IdProveedor { get; set; }

    public string? Nombre { get; set; }

    public DateTime? FechaCreacion { get; set; }

    public string? Observaciones { get; set; }

    public int? IdUsuarioRegistra { get; set; }

    public bool Activo { get; set; }

    public decimal IvaPorDefecto { get; set; } = 21;

    public virtual Proveedor? IdProveedorNavigation { get; set; }

    public virtual Usuario? IdUsuarioRegistraNavigation { get; set; }

    public virtual ICollection<ProductoListaprecioProveedor> ProductoListaprecioProveedors { get; set; } = new List<ProductoListaprecioProveedor>();
}
