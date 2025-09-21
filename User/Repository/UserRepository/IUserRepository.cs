using proyecto_venta_stock.Models;
using proyecto_venta_stock.User.DTO;

namespace proyecto_venta_stock.User.UserRepository
{
    public interface IUserRepository
    {
        Task CreateAsync(Usuario usuario);
        Task<bool> ExistsAsync(string user);
        Task<bool> MailInUseAsync(string mail);
        Task<List<UserDTO>> UsersAsync(int? id);
        IQueryable<Usuario> UsersQueryable(string searchTerm);
    }
}