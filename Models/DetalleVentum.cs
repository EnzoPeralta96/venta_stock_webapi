namespace proyecto_venta_stock.Models;
public partial class DetalleVentum
{
    public int IdVenta { get; set; }

    public int IdProducto { get; set; }

    public decimal? Cantidad { get; set; }

    public decimal? PrecioVenta { get; set; }

    public decimal? SubTotal { get; set; }

    public virtual Producto IdProductoNavigation { get; set; } = null!;

    public virtual Ventum IdVentaNavigation { get; set; } = null!;
}
