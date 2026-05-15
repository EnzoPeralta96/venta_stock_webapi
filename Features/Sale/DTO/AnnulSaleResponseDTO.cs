namespace venta_stock_webapi.Sale.DTO;

public class AnnulSaleResponseDTO
{
    public int IdVenta { get; set; }
    public string CodigoVenta { get; set; } = null!;
    public string Estado { get; set; } = null!;

    /// <summary>
    /// ID del movimiento NC generado en CC.
    /// Null si la venta fue pagada en efectivo (no genera NC en CC).
    /// </summary>
    public int? IdMovimientoNc { get; set; }
}
