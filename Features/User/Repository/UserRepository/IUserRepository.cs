using proyecto_venta_stock.Models;
using proyecto_venta_stock.User.DTO;

namespace proyecto_venta_stock.User.UserRepository
{
    public interface IUserRepository
    {
        Task CreateAsync(Usuario user);
        Task<int> UpdateAsync(Usuario user);
        Task<int> UpdatePasswordAsync(int id, string newPassword);
        Task<Usuario> GetActiveByIdAsync(int id);
        Task<List<UserDTO>> UsersAsync(int? id);
        IQueryable<Usuario> UsersQueryable(string searchTerm);
        Task<int> DeleteAsync(int id);
        Task<int> ActivateAsync(int id);
        Task<Usuario> GetByUserNameAsync(string userName);
        Task<bool> Exists(int id);
        Task<bool> ExistsActive(int id);
        Task<bool> UserNameInUseAsync(int id, string user);
        Task<bool> UserNameInUseAsync(string user);
        Task<bool> MailInUseAsync(string mail);
        Task<bool> MailInUseAsync(int id, string mail);


    }
}