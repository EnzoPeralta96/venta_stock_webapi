using System;
using System.Collections.Generic;

namespace proyecto_venta_stock.Models;

public partial class Producto
{
    public int IdProducto { get; set; }

    public string Nombre { get; set; } = null!;

    public string Marca { get; set; }

    public string Descripcion { get; set; }

    public decimal? Precio { get; set; }

    public int? Stock { get; set; }

    public int? StockMinimo { get; set; }

    public bool? VentaSinStock { get; set; }

    public int? IdUbicacion { get; set; }

    public int? IdCategoria { get; set; }

    public virtual ICollection<CodigoBarra> CodigoBarras { get; set; } = new List<CodigoBarra>();

    public virtual ICollection<Compra> Compras { get; set; } = new List<Compra>();

    public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();

    public virtual Categorium IdCategoriaNavigation { get; set; }

    public virtual Ubicacion IdUbicacionNavigation { get; set; }

    public virtual ICollection<ProductoListaprecioProveedor> ProductoListaprecioProveedors { get; set; } = new List<ProductoListaprecioProveedor>();
}
