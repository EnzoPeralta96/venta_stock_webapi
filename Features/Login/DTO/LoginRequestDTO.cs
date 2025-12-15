using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace venta_stock_webapi.Login.DTO
{
    public class LoginRequestDTO
    {
        [Required (ErrorMessage = "El nombre de usuario es obligatorio.")]
        public string Username { get; set; }
        
        [Required (ErrorMessage = "La contraseña es obligatoria.")]
        public string Password { get; set; }
    }
}