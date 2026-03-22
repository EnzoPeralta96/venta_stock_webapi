using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.CurrentAccount.DTO.MovementDTO
{
    public class UpdateAccountLimitDTO
    {
        [Required]
        public int IdCliente { get; set; }

        [Required]
        public int IdConfiguracion { get; set; }

        [Required]
        public int IdUsuarioRegistra { get; set; }

        [Required]
        [MinLength(5, ErrorMessage = "Debe proporcionar un motivo válido.")]
        public string Motivo { get; set; }
    }
}
