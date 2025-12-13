

namespace proyecto_venta_stock.Models;
public partial class TipoMovimiento
{
    //En realidad era IdTipoMovimiento.
    public int IdMovimiento { get; set; }

    public string? Nombre { get; set; }

    public string? Accion { get; set; }

    public virtual ICollection<MovimientoCc> MovimientoCcs { get; set; } = new List<MovimientoCc>();
}
