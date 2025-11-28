
namespace proyecto_venta_stock.Models;
public partial class PermisoUsuario
{
    public int IdPermiso { get; set; }

    public int IdUsuario { get; set; }

    public DateOnly? FechaAsignacion { get; set; }

    public virtual Permiso IdPermisoNavigation { get; set; } = null!;

    public virtual Usuario IdUsuarioNavigation { get; set; } = null!;
}
