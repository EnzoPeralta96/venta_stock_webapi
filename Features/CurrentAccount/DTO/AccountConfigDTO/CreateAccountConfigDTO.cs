
using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.CurrentAccount.DTO
{
    public class CreateAccountConfigDTO
    {
        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = null!;
        
        [Required(ErrorMessage = "El monto limite es obligatorio.")]
        public decimal MontoLimite { get; set; }
    }
}