using System;
using System.Collections.Generic;

namespace proyecto_venta_stock.Models;

public partial class MovimientoCc
{
    public int IdMovimiento { get; set; }

    public decimal? Importe { get; set; }

    public DateTime? Fecha { get; set; }

    public string? Detalle { get; set; }

    public int? IdEstado { get; set; }

    public decimal? SaldoActual { get; set; }

    public decimal? LimiteCuenta { get; set; }

    public int? IdTipoMovimiento { get; set; }

    public DateTime? FechaAutorizacion { get; set; }

    public int? IdUsuarioAutoriza { get; set; }

    public int? IdVenta { get; set; }

    public int? IdCliente { get; set; }

    public int? IdUsuarioRegistra { get; set; }

    /// <summary>
    /// Solo relevante para movimientos de tipo movimiento_cc.
    /// Acumula el total pagado contra este consumo (pago_factura o allocación de pago_global).
    /// Permite derivar EstadoPago: pendiente / parcial / pagado.
    /// </summary>
    public decimal? MontoPagado { get; set; }

    /// <summary>
    /// Indica si este movimiento de pago fue anulado.
    /// Solo relevante para tipos pago_global (6) y pago_factura (8).
    /// </summary>
    public bool EsAnulado { get; set; }

    public virtual Cliente? IdClienteNavigation { get; set; }

    public virtual Estado? IdEstadoNavigation { get; set; }

    public virtual TipoMovimiento? IdTipoMovimientoNavigation { get; set; }

    public virtual Usuario? IdUsuarioAutorizaNavigation { get; set; }

    public virtual Usuario? IdUsuarioRegistraNavigation { get; set; }

    public virtual Ventum? IdVentaNavigation { get; set; }

    /// <summary>
    /// Solo relevante para movimientos de tipo NOTA_DEBITO (3).
    /// Referencia al motivo de nota de débito seleccionado.
    /// </summary>
    public int? IdMotivoNd { get; set; }
    public virtual MotivoNotaDebito? IdMotivoNdNavigation { get; set; }

    public int? IdMotivoNc { get; set; }
    public virtual MotivoNotaCredito? IdMotivoNcNavigation { get; set; }
}
