namespace venta_stock_webapi.CurrentAccount.DTO.InterestConfigDTO;

public class InterestConfigDTO
{
    public int IdConfig { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal PorcentajeInteres { get; set; }
    public int DiaVencimiento { get; set; }
    public bool EsActual { get; set; }
}
