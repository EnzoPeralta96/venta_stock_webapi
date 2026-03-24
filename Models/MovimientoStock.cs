namespace proyecto_venta_stock.Models;

public enum TipoMovimientoStockEnum
{
    IngresoCompra = 1,
    EgresoVenta = 2,
    ReingresoAnulacionVenta = 3,
    EgresoAnulacionCompra = 4,
    AjustePositivoManual = 5,
    AjusteNegativoManual = 6,
    ConsumoInternoDueno = 7
}

public class TipoMovimientoStock
{
    public int IdTipoMovimientoStock { get; set; }
    public string Nombre { get; set; } = null!;
    public string Descripcion { get; set; } = null!;

    public virtual ICollection<MovimientoStock> MovimientosStock { get; set; } = new List<MovimientoStock>();
}

public class MovimientoStock
{
    public int IdMovimientoStock { get; set; }
    public int IdProducto { get; set; }
    public int IdTipoMovimientoStock { get; set; }

    /// <summary>
    /// Cantidad del movimiento. Positiva para ingresos, negativa para egresos.
    /// </summary>
    public decimal Cantidad { get; set; }

    /// <summary>
    /// Caché del stock resultante al momento del movimiento.
    /// </summary>
    public decimal StockResultante { get; set; }

    public DateTime Fecha { get; set; }
    public int? IdUsuario { get; set; }
    public string? Referencia { get; set; }

    public virtual Producto IdProductoNavigation { get; set; } = null!;
    public virtual TipoMovimientoStock IdTipoMovimientoStockNavigation { get; set; } = null!;
    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
