using System;
using System.Collections.Generic;

namespace proyecto_venta_stock.Models;

public partial class Ubicacion
{
    public int IdUbicacion { get; set; }

    public int? Fila { get; set; }

    public string Seccion { get; set; }

    public int? Nivel { get; set; }

    public virtual ICollection<Producto> Productos { get; set; } = new List<Producto>();
}
