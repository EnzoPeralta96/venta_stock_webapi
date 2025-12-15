using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace venta_stock_webapi.Login.DTO
{
    public class LoginRequestDTO
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}