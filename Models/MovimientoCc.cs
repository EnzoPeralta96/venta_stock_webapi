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

    public virtual Cliente? IdClienteNavigation { get; set; }

    public virtual Estado? IdEstadoNavigation { get; set; }

    public virtual TipoMovimiento? IdTipoMovimientoNavigation { get; set; }

    public virtual Usuario? IdUsuarioAutorizaNavigation { get; set; }

    public virtual Usuario? IdUsuarioRegistraNavigation { get; set; }

    public virtual Ventum? IdVentaNavigation { get; set; }
}
