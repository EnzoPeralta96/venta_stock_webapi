using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.User.DTO;
using venta_stock_webapi.Shared.Paged;

namespace proyecto_venta_stock.Services
{
    public interface IUserService
    {
        Task<Result<bool>> CreateAsync(UserCreateDTO userDTO);
        Task<Result<PagedList<UserDTO>>> UsersPagedAsync(int pageIndex, int pageSize, string searchTerm);
        Task<Result<List<UserDTO>>> GetUsersAsync(int? id);

    }
}