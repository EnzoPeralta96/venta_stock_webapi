namespace proyecto_venta_stock.ListaPrecio.DTO;

public class ListaPrecioDTO
{
    public int IdLista { get; set; }
    public int? IdProveedor { get; set; }
    public string? Nombre { get; set; }
    public DateTime? FechaCreacion { get; set; }
    public string? Observaciones { get; set; }
    public bool Activo { get; set; }
    public int? IdUsuarioRegistra { get; set; }
    public int CantidadItems { get; set; }
    public decimal IvaPorDefecto { get; set; }
}
