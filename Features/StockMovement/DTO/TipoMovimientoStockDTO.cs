namespace venta_stock_webapi.Features.StockMovement.DTO;

public class TipoMovimientoStockDTO
{
    public int IdTipoMovimientoStock { get; set; }
    public string Nombre { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    public bool Activo { get; set; }
    public bool EsSistema { get; set; }
    public bool EsPositivo { get; set; }
}
