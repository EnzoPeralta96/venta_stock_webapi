namespace proyecto_venta_stock.Models;
public partial class Estado
{
    public int IdEstado { get; set; }

    public string? Estado1 { get; set; }

    public virtual ICollection<MovimientoCc> MovimientoCcs { get; set; } = new List<MovimientoCc>();

    public virtual ICollection<Ventum> Venta { get; set; } = new List<Ventum>();
}
