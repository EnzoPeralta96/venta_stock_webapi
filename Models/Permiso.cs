using System;
using System.Collections.Generic;

namespace proyecto_venta_stock.Models;

public partial class Permiso
{
    public int IdPermiso { get; set; }
    public string? Permiso1 { get; set; }
    public string? Descripcion { get; set; }
    public int IdCategoriaPermiso { get; set; }   // FK a CategoriaPermiso
    public virtual CategoriaPermiso CategoriaPermiso { get; set; } = null!;
    public virtual ICollection<PermisoUsuario> PermisoUsuarios { get; set; } = new List<PermisoUsuario>();
}
