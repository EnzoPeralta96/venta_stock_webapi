using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.Client.DTO
{
    public class ClientToggleStatusDTO
    {
        [Required(ErrorMessage = "El ID del cliente es obligatorio.")]
        public int IdCliente { get; set; }
        
        [Required(ErrorMessage = "El estado activo es obligatorio.")]
        public bool IsActive { get; set; }
    }
}