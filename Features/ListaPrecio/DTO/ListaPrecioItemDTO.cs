namespace proyecto_venta_stock.ListaPrecio.DTO;

public class ListaPrecioItemDTO
{
    public int IdLista { get; set; }
    public int IdProducto { get; set; }
    public decimal Precio { get; set; }
    public decimal? Margen { get; set; }
    public string? NombreProducto { get; set; }
    public string? Marca { get; set; }
    public int? IdUnidadMedida { get; set; }
}
