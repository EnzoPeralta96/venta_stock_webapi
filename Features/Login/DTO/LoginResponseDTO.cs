using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace venta_stock_webapi.Login.DTO
{
    public class LoginResponseDTO
    {
        public string Token { get; set; }
        public DateTime Expiration { get; set; }
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;   
        public bool Root { get; set; } 
        public List<string> Permissions { get; set; } = new List<string>(); 
    }
}