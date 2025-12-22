namespace venta_stock_webapi.Features.Ferreteria.DTO
{
    public class FerreteriaResponseDTO
    {
        public int IdFerreteria { get; set; }
        public string Nombre { get; set; } = null!;
        public string Direccion { get; set; } = null!;
        public string Telefono { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Cuit { get; set; } = null!;
        public string? LogoUrl { get; set; }
        public DateTime FechaActualizacion { get; set; }
    }
}
