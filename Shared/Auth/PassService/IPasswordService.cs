using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Shared.Auth.PassService
{
    public interface IPasswordService
    {
        string HashPassword(Usuario user, string plainPassword);
        bool VerifyPassword(Usuario user, string providedPassword);
    }
}