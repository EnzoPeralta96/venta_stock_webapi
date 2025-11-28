namespace proyecto_venta_stock.Models;
public partial class MedioPago
{
    public int IdMedioPago { get; set; }

    public string? MedioPago1 { get; set; }

    public virtual ICollection<Ventum> Venta { get; set; } = new List<Ventum>();
}
