using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.User.UserRepository
{
    public interface IUserRepository
    {
        Task Create(Usuario usuario);
        Task<bool> Exists(string user);
        Task<bool> MailInUse(string mail);
    }
}