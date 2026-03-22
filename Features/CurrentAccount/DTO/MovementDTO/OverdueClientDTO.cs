namespace venta_stock_webapi.CurrentAccount.DTO.MovementDTO;

public class OverdueClientDTO
{
    public int IdCliente { get; set; }
    public string NombreCliente { get; set; } = null!;
    public string? Dni { get; set; }
    public string? Cuit { get; set; }
    public decimal SaldoDeudor { get; set; }
    public decimal ImporteInteres { get; set; }
    public decimal PorcentajeInteres { get; set; }
    public int DiaVencimiento { get; set; }
}
