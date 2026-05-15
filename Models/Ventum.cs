namespace proyecto_venta_stock.Models;
public partial class Ventum
{
    public int IdVenta { get; set; }
    public string CodigoVenta { get; set; }

    public DateTime? Fecha { get; set; }

    public decimal? Total { get; set; }

    public int? IdMedioPago { get; set; }

    public int? IdCliente { get; set; }

    public int? IdUsuario { get; set; }

    public int? IdEstado { get; set; }

    /// <summary>Motivo de la nota de crédito cuando la venta es anulada. Null si no está anulada.</summary>
    public int? IdMotivoNc { get; set; }
    public virtual MotivoNotaCredito? IdMotivoNcNavigation { get; set; }

    /// <summary>Detalle adicional opcional ingresado al anular la venta.</summary>
    public string? DetalleNc { get; set; }

    public virtual ICollection<DetalleVentum> DetalleVenta { get; set; } = new List<DetalleVentum>();

    public virtual Cliente? IdClienteNavigation { get; set; }

    public virtual Estado? IdEstadoNavigation { get; set; }

    public virtual MedioPago? IdMedioPagoNavigation { get; set; }

    public virtual Usuario? IdUsuarioNavigation { get; set; }

    public virtual ICollection<MovimientoCc> MovimientoCcs { get; set; } = new List<MovimientoCc>();
}
