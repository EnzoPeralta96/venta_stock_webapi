namespace proyecto_venta_stock.Models;

public class MotivoNotaCredito
{
    public int IdMotivo { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activo { get; set; } = true;
}
