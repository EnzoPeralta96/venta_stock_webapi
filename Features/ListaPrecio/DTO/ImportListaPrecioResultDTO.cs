namespace proyecto_venta_stock.ListaPrecio.DTO;

public class ImportListaPrecioResultDTO
{
    public int TotalProcesados { get; set; }
    public int Insertados { get; set; }
    public int Actualizados { get; set; }
    public List<string> Errores { get; set; } = [];
}
