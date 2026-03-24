namespace proyecto_venta_stock.Models;

public partial class CompraProveedorDetalle
{
    public int IdCompraProveedorDetalle { get; set; }

    public int IdCompraProveedor { get; set; }

    public int IdProducto { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal DescuentoPorcentaje { get; set; }

    public decimal IvaPorcentaje { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Total { get; set; }

    public virtual CompraProveedor IdCompraProveedorNavigation { get; set; } = null!;

    public virtual Producto IdProductoNavigation { get; set; } = null!;
}
