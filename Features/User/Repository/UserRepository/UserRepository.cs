using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.User.DTO;

namespace proyecto_venta_stock.User.UserRepository
{
    public class UserRepository : IUserRepository
    {
        private readonly VentaStockContext _dbContext;
        public UserRepository(VentaStockContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IQueryable<Usuario> UsersQueryable(string searchTerm)
        {
            var query = _dbContext.Usuarios
                    .AsNoTracking()
                    .OrderBy(u => u.FechaBaja != null)
                        .ThenBy(u => u.Apellido)
                        .ThenBy(u => u.Nombre)
                    .AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                query = query.Where(
                    c => c.Apellido.ToLower().Contains(searchTerm) ||
                    c.Nombre.ToLower().Contains(searchTerm) ||
                    (c.Nombre + " " + c.Apellido).ToLower().Contains(searchTerm)
                );
            }
            return query;
        }

        public async Task<List<UserDTO>> UsersAsync(int? id)
        {
            var query = _dbContext.Usuarios.AsQueryable();

            if (id.HasValue)
            {
                query = query.Where(u => u.IdUsuario == id.Value);
            }

            return await query.Select(u => new UserDTO
            {
                IdUsuario = u.IdUsuario,
                Usuario = u.Usuario1,
                Password = u.Password,
                Nombre = u.Nombre,
                Apellido = u.Apellido,
                Email = u.Email,
                Rol = u.Rol,
                Permisos = u.PermisoUsuarios
                        .GroupBy(pu => pu.IdPermisoNavigation.CategoriaPermiso)
                        .Select(g => new PermissionsCategoryDTO
                        {
                            IdCategoriaPermiso = g.Key.IdCategoriaPermiso,
                            Categoria = g.Key.Categoria,
                            Permissions = g.Select(pu => new PermissionDTO
                            {
                                IdPermiso = pu.IdPermisoNavigation.IdPermiso,
                                Permiso = pu.IdPermisoNavigation.Permiso1!,
                                Descripcion = pu.IdPermisoNavigation.Descripcion
                            }).ToList()
                        }).ToList()
            })
            .AsNoTracking()
            .ToListAsync();
        }


        public async Task CreateAsync(Usuario usuario)
        {
            await _dbContext.AddAsync(usuario);
            await _dbContext.SaveChangesAsync();
        }

        public Task<int> UpdateAsync(Usuario user)
        {
            return _dbContext.Usuarios
            .Where(u => u.IdUsuario == user.IdUsuario)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.Usuario1, user.Usuario1)
                .SetProperty(u => u.Password, user.Password)
                .SetProperty(u => u.Nombre, user.Nombre)
                .SetProperty(u => u.Apellido, user.Apellido)
                .SetProperty(u => u.Email, user.Email)
                .SetProperty(u => u.Rol, user.Rol)
            );
        }

        public Task<int> DeleteAsync(int id)
        {
            return _dbContext.Usuarios
            .Where(u => u.IdUsuario == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(u => u.FechaBaja, DateOnly.FromDateTime(DateTime.Now))
            );
        }
        public Task<bool> UserNameInUseAsync(string user)
        {
            return _dbContext.Usuarios.AnyAsync(u => u.Usuario1 == user);
        }

        public Task<bool> UserNameInUseAsync(int id, string user)
        {
            return _dbContext.Usuarios.AnyAsync(u => u.Usuario1 == user && u.IdUsuario != id);
        }
        public Task<bool> MailInUseAsync(string mail)
        {
            return _dbContext.Usuarios.AnyAsync(u => u.Email == mail);
        }
        public Task<bool> MailInUseAsync(int id, string mail)
        {
            return _dbContext.Usuarios.AnyAsync(u => u.Email == mail && u.IdUsuario != id);
        }
        public Task<bool> ExistsActive(int id)
        {
            return _dbContext.Usuarios.AnyAsync(u => u.IdUsuario == id && u.FechaBaja == null);
        }

        public Task<bool> Exists(int id)
        {
            return _dbContext.Usuarios.AnyAsync(u => u.IdUsuario == id);
        }

        public Task<Usuario> GetByUserNameAsync(string userName)
        {
            return _dbContext.Usuarios
                .Where(u => u.Usuario1 == userName && u.FechaBaja == null)
                .Include(u => u.PermisoUsuarios)
                    .ThenInclude(pu => pu.IdPermisoNavigation)
                .FirstOrDefaultAsync();
        }

    }
}