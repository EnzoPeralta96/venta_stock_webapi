
namespace venta_stock_webapi.CurrentAccount.DTO
{
    public class AccountConfigDTO
    {
        public int IdConfig { get; set; }
        public string Nombre { get; set; } = null!;
        public decimal MontoLimite { get; set; }
        public bool Activo { get; set; }
    }
}