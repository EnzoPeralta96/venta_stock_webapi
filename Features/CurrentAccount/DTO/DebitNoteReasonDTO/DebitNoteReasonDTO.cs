namespace venta_stock_webapi.CurrentAccount.DTO.DebitNoteReasonDTO;

public class DebitNoteReasonDTO
{
    public int IdMotivo { get; set; }
    public string Nombre { get; set; } = null!;
    public bool Activo { get; set; }
    public string Categoria { get; set; } = null!;
}
