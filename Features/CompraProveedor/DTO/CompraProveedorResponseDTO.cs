namespace proyecto_venta_stock.CompraProveedor.DTO;

public class CompraProveedorResponseDTO
{
    public int IdCompraProveedor { get; set; }
    public int IdProveedor { get; set; }
    public string NombreProveedor { get; set; } = string.Empty;
    public DateOnly Fecha { get; set; }
    public DateOnly? FechaVencimiento { get; set; }
    public string? TipoComprobante { get; set; }
    public string? NumeroComprobante { get; set; }
    public string? Observacion { get; set; }
    public decimal Subtotal { get; set; }
    public decimal DescuentoTotal { get; set; }
    public decimal IvaTotal { get; set; }
    public decimal Total { get; set; }
    public int IdUsuario { get; set; }
    public string? NombreUsuario { get; set; }
    public bool Activo { get; set; }
}

public class CompraProveedorDetailResponseDTO : CompraProveedorResponseDTO
{
    public List<CompraProveedorDetalleResponseDTO> Detalles { get; set; } = new();
}

public class CompraProveedorDetalleResponseDTO
{
    public int IdCompraProveedorDetalle { get; set; }
    public int IdProducto { get; set; }
    public string NombreProducto { get; set; } = string.Empty;
    public int? IdUnidadMedida { get; set; }
    public decimal Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public decimal DescuentoPorcentaje { get; set; }
    public decimal IvaPorcentaje { get; set; }
    public decimal Subtotal { get; set; }
    public decimal Total { get; set; }
}
