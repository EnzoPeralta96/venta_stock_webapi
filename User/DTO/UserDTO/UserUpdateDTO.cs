
using System.ComponentModel.DataAnnotations;


namespace proyecto_venta_stock.User.DTO
{
    public class UserUpdateDTO
    {
        [Required(ErrorMessage = "El Identificador de usuario es requerido")]
        public int IdUsuario { get; set; }
        [Required(ErrorMessage = "El nombre de usuario es requerido")]
        public string Usuario { get; set; }

        [Required(ErrorMessage = "La contraseña de usuario es requerida")]
        public string Password { get; set; }

        [Required(ErrorMessage = "El nombre es requerido")]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El nombre apellido es requerido")]
        public string Apellido { get; set; }

        [Required(ErrorMessage = "El correo de usuario es requerido")]
        public string Email { get; set; }

        [Required(ErrorMessage = "El rol de usuario es requerido")]
        public string Rol { get; set; }
    }
}