namespace proyecto_venta_stock.Models
{
    public partial class ConfiguracionCc
    {
        public int IdConfig { get; set; }
        public string Nombre { get; set; } = null!;
        public decimal MontoLimite { get; set; }
        public bool Activo { get; set; } = true;
    }
}