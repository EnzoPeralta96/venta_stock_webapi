using System;
using System.Collections.Generic;

namespace proyecto_venta_stock.Models;

public partial class CodigoBarra
{
    public int IdCodigo { get; set; }

    public string Codigo { get; set; } = null!;

    public bool? Activo { get; set; }

    public bool? Prinicial { get; set; }

    public int? IdProducto { get; set; }

    public virtual Producto? IdProductoNavigation { get; set; }
}
