namespace proyecto_venta_stock.Models;
public partial class Categorium
{
    public int IdCategoria { get; set; }

    public string Categoria { get; set; } = null!;

    public string? Descripcion { get; set; }
    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
