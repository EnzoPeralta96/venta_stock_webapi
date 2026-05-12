namespace proyecto_venta_stock.ListaPrecio.DTO;

public class BulkAddResultDTO
{
    public int Insertados { get; set; }
    public int Actualizados { get; set; }
    public List<int> Ignorados { get; set; } = new();
}
