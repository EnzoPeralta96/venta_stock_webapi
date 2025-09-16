using System;
using System.Collections.Generic;

namespace proyecto_venta_stock.Models;

public partial class Compra
{
    public int IdProducto { get; set; }

    public int IdProveedor { get; set; }

    public DateOnly Fecha { get; set; }

    public decimal? PrecioUnitario { get; set; }

    public int? Cantidad { get; set; }

    public int? IdUsuario { get; set; }

    public virtual Producto IdProductoNavigation { get; set; } = null!;

    public virtual Proveedor IdProveedorNavigation { get; set; } = null!;

    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
