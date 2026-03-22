using System.ComponentModel.DataAnnotations;


namespace venta_stock_webapi.Features.User.DTO.UserDTO
{
    public class UserChangePasswordDTO
    {
        [Required(ErrorMessage = "El Identificador de usuario es requerido")]
        public int IdUsuario { get; set; }

        [Required(ErrorMessage = "La contraseña nueva es requerida")]
        public string NewPassword { get; set; } = string.Empty;
    }
}