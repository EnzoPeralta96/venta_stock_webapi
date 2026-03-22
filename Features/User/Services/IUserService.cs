using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.User.DTO;
using venta_stock_webapi.Features.User.DTO.UserDTO;
using venta_stock_webapi.Shared.Paged;

namespace proyecto_venta_stock.Services
{
    public interface IUserService
    {
        Task<Result<bool>> CreateAsync(UserCreateDTO userDTO);
        Task<Result<bool>> UpdateAsync(UserUpdateDTO userDTO);
        Task<Result<bool>> ChangePasswordAsync(UserChangePasswordDTO dto);
        Task<Result<bool>> DeleteAsync(int id);
        Task<Result<bool>> ActivateAsync(int id);
        Task<Result<PagedList<UserDTO>>> UsersPagedAsync(int pageIndex, int pageSize, string searchTerm, string estado);
        Task<Result<List<UserDTO>>> GetUsersAsync(int? id);
    }
}