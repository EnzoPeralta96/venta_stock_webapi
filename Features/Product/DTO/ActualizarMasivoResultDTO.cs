namespace proyecto_venta_stock.Product.DTO;

public class ActualizarMasivoResultDTO
{
    public int Actualizados { get; set; }
    public List<int> Ignorados { get; set; } = new();
    public List<string> CodigosIgnorados { get; set; } = new();
    public List<ActualizarPrecioResultItemDTO> Detalle { get; set; } = new();
}

public class ActualizarPrecioResultItemDTO
{
    public int IdProducto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public decimal CostoAnterior { get; set; }
    public decimal CostoNuevo { get; set; }
    public decimal PrecioAnterior { get; set; }
    public decimal PrecioNuevo { get; set; }
}
