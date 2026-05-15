namespace proyecto_venta_stock.Models;
public partial class CategoriaPermiso
{
    public int IdCategoriaPermiso { get; set; }
    public string Categoria { get; set; } = null!;
    public virtual ICollection<Permiso> Permisos { get; set; } = new List<Permiso>();
}
