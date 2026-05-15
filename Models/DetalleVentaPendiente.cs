using System;
using System.Collections.Generic;

namespace proyecto_venta_stock.Models;

public partial class DetalleVentaPendiente
{
    public int IdDetalle { get; set; }

    public int IdVentaPendiente { get; set; }

    public int IdProducto { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioVenta { get; set; }

    public decimal Subtotal { get; set; }

    public virtual VentaPendiente IdVentaPendienteNavigation { get; set; }

    public virtual Producto IdProductoNavigation { get; set; }
}
