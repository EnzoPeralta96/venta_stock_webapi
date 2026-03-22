namespace proyecto_venta_stock.Models;

public class ConfiguracionInteres
{
    public int IdConfig { get; set; }

    /// <summary>Nombre descriptivo, ej. "Interés Marzo 2025"</summary>
    public string Nombre { get; set; } = null!;

    /// <summary>Porcentaje a aplicar sobre el saldo deudor. Ej: 5.00 = 5%</summary>
    public decimal PorcentajeInteres { get; set; }

    /// <summary>
    /// Día del mes hasta el cual el cliente puede pagar sin mora.
    /// El sistema considera vencida la deuda a partir del día siguiente.
    /// Ej: 10 → vence el día 11.
    /// </summary>
    public int DiaVencimiento { get; set; }

    /// <summary>
    /// Solo UNA configuración puede tener EsActual = true a la vez.
    /// Es la configuración vigente del sistema.
    /// </summary>
    public bool EsActual { get; set; } = false;
}
