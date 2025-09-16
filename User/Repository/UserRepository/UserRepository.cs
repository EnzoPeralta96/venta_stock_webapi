using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;

namespace proyecto_venta_stock.User.UserRepository
{
    public class UserRepository : IUserRepository
    {
        private readonly VentaStockContext _dbContext;
        public UserRepository(VentaStockContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task Create(Usuario usuario)
        {
            await _dbContext.AddAsync(usuario);
            await _dbContext.SaveChangesAsync();
        }

        public Task<bool> Exists(string user)
        {
            return _dbContext.Usuarios.AnyAsync(u => u.Usuario1 == user && u.FechaBaja != null);
        }

        public Task<bool> MailInUse(string mail)
        {
             return _dbContext.Usuarios.AnyAsync(u => u.Email == mail && u.FechaBaja != null);
        }
    }
}