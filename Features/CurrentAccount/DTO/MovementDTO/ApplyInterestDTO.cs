using System.ComponentModel.DataAnnotations;

namespace venta_stock_webapi.CurrentAccount.DTO.MovementDTO;

public class ApplyInterestDTO
{
    [Required]
    public int IdUsuarioRegistra { get; set; }
}
