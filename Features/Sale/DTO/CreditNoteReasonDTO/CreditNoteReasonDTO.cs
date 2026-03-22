namespace venta_stock_webapi.Sale.DTO.CreditNoteReasonDTO;

public class CreditNoteReasonDTO
{
    public int IdMotivo { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activo { get; set; }
}
