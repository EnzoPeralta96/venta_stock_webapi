using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.CurrentAccount.DTO.MovementDTO
{
    public class AnnulPaymentDTO
    {
        [Required(ErrorMessage = "El ID del movimiento a anular es obligatorio.")]
        public int IdMovimientoPago { get; set; }

        [Required(ErrorMessage = "El ID del usuario es obligatorio.")]
        public int IdUsuarioRegistra { get; set; }

        [Required(ErrorMessage = "El motivo de anulación es obligatorio.")]
        [MinLength(10, ErrorMessage = "El motivo debe tener al menos 10 caracteres.")]
        public string Motivo { get; set; }
    }
}
