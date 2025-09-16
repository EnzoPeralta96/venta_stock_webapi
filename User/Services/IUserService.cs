using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.User.DTO;

namespace proyecto_venta_stock.Services
{
    public interface IUserService
    {
        Task<Result<bool>> Create(UserDTO userDTO);
        Task<Result<List<UserDTO>>> Users();
    }
}