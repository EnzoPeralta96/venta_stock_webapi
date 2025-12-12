
using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.CurrentAccount.DTO
{
    public class UpdateAccountConfigDTO
    {
        [Required(ErrorMessage = "El ID de la configuración es obligatorio.")]
        public int IdConfig { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio.")]
        public string Nombre { get; set; } = null!;
        
        [Required(ErrorMessage = "El monto limite es obligatorio.")]
        public decimal MontoLimite { get; set; }
    }
}