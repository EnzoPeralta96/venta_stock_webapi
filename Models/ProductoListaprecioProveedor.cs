using System;
using System.Collections.Generic;

namespace proyecto_venta_stock.Models;

public partial class ProductoListaprecioProveedor
{
    public int IdLista { get; set; }

    public int IdProducto { get; set; }

    public decimal Precio { get; set; }

    public decimal? Margen { get; set; }

    public virtual ListaPrecio IdListaNavigation { get; set; } = null!;

    public virtual Producto IdProductoNavigation { get; set; } = null!;
}
