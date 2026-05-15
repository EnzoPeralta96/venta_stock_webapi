namespace venta_stock_webapi.Features.UnidadMedida.DTO;

public class UnidadMedidaDTO
{
    public int IdUnidadMedida { get; set; }
    public string Nombre { get; set; } = null!;
    public string Abreviatura { get; set; } = null!;
    public bool Activo { get; set; }
}
