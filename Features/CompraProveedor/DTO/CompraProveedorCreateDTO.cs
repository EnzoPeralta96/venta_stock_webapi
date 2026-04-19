using System.ComponentModel.DataAnnotations;

namespace proyecto_venta_stock.CompraProveedor.DTO;

public class CompraProveedorCreateDTO
{
    [Required(ErrorMessage = "El proveedor es obligatorio.")]
    public int IdProveedor { get; set; }

    [Required(ErrorMessage = "La fecha es obligatoria.")]
    public DateOnly Fecha { get; set; }

    public DateOnly? FechaVencimiento { get; set; }

    public string? TipoComprobante { get; set; }

    public string? NumeroComprobante { get; set; }

    public string? Observacion { get; set; }

    public int IdUsuario { get; set; }

    [Required(ErrorMessage = "Debe incluir al menos un detalle de compra.")]
    [MinLength(1, ErrorMessage = "Debe incluir al menos un detalle de compra.")]
    public List<CompraProveedorDetalleCreateDTO> Detalles { get; set; } = new();
}

public class CompraProveedorDetalleCreateDTO
{
    [Required(ErrorMessage = "El producto es obligatorio.")]
    public int IdProducto { get; set; }

    [Range(0.001, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor a cero.")]
    public decimal Cantidad { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "El precio unitario no puede ser negativo.")]
    public decimal PrecioUnitario { get; set; }

    [Range(0, 100, ErrorMessage = "El descuento debe estar entre 0 y 100.")]
    public decimal DescuentoPorcentaje { get; set; }

    [Range(0, 100, ErrorMessage = "El IVA debe estar entre 0 y 100.")]
    public decimal IvaPorcentaje { get; set; }

    public decimal? MargenAplicado { get; set; }
}
