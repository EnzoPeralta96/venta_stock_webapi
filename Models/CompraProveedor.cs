namespace proyecto_venta_stock.Models;

public partial class CompraProveedor
{
    public int IdCompraProveedor { get; set; }

    public int IdProveedor { get; set; }

    public DateOnly Fecha { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public string? TipoComprobante { get; set; }

    public string? NumeroComprobante { get; set; }

    public string? Observacion { get; set; }

    public decimal Subtotal { get; set; }

    public decimal DescuentoTotal { get; set; }

    public decimal IvaTotal { get; set; }

    public decimal Total { get; set; }

    public int? IdUsuario { get; set; }

    public bool Activo { get; set; } = true;

    public virtual Proveedor IdProveedorNavigation { get; set; } = null!;

    public virtual Usuario? IdUsuarioNavigation { get; set; }

    public virtual ICollection<CompraProveedorDetalle> CompraProveedorDetalles { get; set; } = new List<CompraProveedorDetalle>();
}
