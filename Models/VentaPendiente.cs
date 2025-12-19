using System;
using System.Collections.Generic;

namespace proyecto_venta_stock.Models;

/// <summary>
/// Ventas que exceden el límite de crédito y requieren autorización
/// </summary>
public partial class VentaPendiente
{
    public int IdVentaPendiente { get; set; }

    public string CodigoVenta { get; set; }

    public int IdCliente { get; set; }

    public int IdUsuarioVendedor { get; set; }

    public int IdMedioPago { get; set; }

    public decimal Total { get; set; }

    public decimal? SaldoActual { get; set; }

    public decimal? LimiteCuenta { get; set; }

    public decimal? SaldoDespuesVenta { get; set; }

    /// <summary>
    /// Monto que excede el límite de crédito
    /// </summary>
    public decimal? Excedente { get; set; }

    public int IdEstado { get; set; }

    public int? IdUsuarioAutoriza { get; set; }

    public DateTime? FechaAutorizacion { get; set; }

    public string ObservacionesAutorizacion { get; set; }

    public DateTime FechaRegistro { get; set; }

    /// <summary>
    /// Referencia a la venta definitiva si fue aprobada
    /// </summary>
    public int? IdVentaGenerada { get; set; }

    public virtual ICollection<DetalleVentaPendiente> DetalleVentaPendientes { get; set; } = new List<DetalleVentaPendiente>();

    // Navigation properties
    public virtual Cliente IdClienteNavigation { get; set; }

    public virtual Usuario IdUsuarioVendedorNavigation { get; set; }

    public virtual MedioPago IdMedioPagoNavigation { get; set; }

    public virtual Estado IdEstadoNavigation { get; set; }

    public virtual Usuario IdUsuarioAutorizaNavigation { get; set; }

    public virtual Ventum IdVentaGeneradaNavigation { get; set; }
}
