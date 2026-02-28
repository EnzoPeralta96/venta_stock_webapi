namespace proyecto_venta_stock.Proveedor.DTO
{
    public class ProveedorDTO
    {
        public int IdProveedor { get; set; }
        public string Nombre { get; set; } = null!;
        public string? Direccion { get; set; }
        public string? Telefono { get; set; }
    }
}