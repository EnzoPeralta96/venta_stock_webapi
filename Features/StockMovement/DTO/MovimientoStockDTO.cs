namespace venta_stock_webapi.Features.StockMovement.DTO;

public class MovimientoStockDTO
{
    public int IdMovimientoStock { get; set; }
    public int IdProducto { get; set; }
    public int IdTipoMovimientoStock { get; set; }
    public string TipoMovimiento { get; set; } = null!;
    public decimal Cantidad { get; set; }
    public decimal StockResultante { get; set; }
    public DateTime Fecha { get; set; }
    public string? Usuario { get; set; }
    public string? Referencia { get; set; }
}
