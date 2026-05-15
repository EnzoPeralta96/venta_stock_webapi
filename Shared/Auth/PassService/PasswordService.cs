using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using proyecto_venta_stock.Models;

namespace venta_stock_webapi.Shared.Auth.PassService
{
    public class PasswordService : IPasswordService
    {
        private readonly IPasswordHasher<Usuario> _hasher;

        public PasswordService(IPasswordHasher<Usuario> hasher)
        {
            _hasher = hasher;
        }

        public string HashPassword(Usuario user, string plainPassword) => _hasher.HashPassword(user, plainPassword);
            
        public bool VerifyPassword(Usuario user, string providedPassword)
        {
            var result = _hasher.VerifyHashedPassword(
                user, user.Password, providedPassword
            );
            return result != PasswordVerificationResult.Failed;
        }
    }
}