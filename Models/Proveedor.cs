namespace proyecto_venta_stock.Models;
public partial class Proveedor
{
    public int IdProveedor { get; set; }

    public string Proveedor1 { get; set; } = null!;

    public string? Direccion { get; set; }

    public string? Telefono { get; set; }
     public bool Activo { get; set; } = true;

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();

    public virtual ICollection<ListaPrecio> ListaPrecios { get; set; } = new List<ListaPrecio>();
}
