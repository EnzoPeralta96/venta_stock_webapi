namespace proyecto_venta_stock.Models;
public partial class Usuario
{
    public int IdUsuario { get; set; }

    public string Usuario1 { get; set; } = null!;

    public string Password { get; set; } = null!;
    
    public string Nombre { get; set; }

    public string Apellido { get; set; }

    public string Email { get; set; }

    public DateOnly? FechaAlta { get; set; }

    public DateOnly? FechaBaja { get; set; }

    public string Rol { get; set; }
    public bool Root { get; set; } = false;

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();

    public virtual ICollection<ListaPrecio> ListaPrecios { get; set; } = new List<ListaPrecio>();

    public virtual ICollection<MovimientoCc> MovimientoCcIdUsuarioAutorizaNavigations { get; set; } = new List<MovimientoCc>();

    public virtual ICollection<MovimientoCc> MovimientoCcIdUsuarioRegistraNavigations { get; set; } = new List<MovimientoCc>();

    public virtual ICollection<PermisoUsuario> PermisoUsuarios { get; set; } = new List<PermisoUsuario>();

    public virtual ICollection<Ventum> Venta { get; set; } = new List<Ventum>();
}
