using AutoMapper;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.User.DTO;
using proyecto_venta_stock.User.Repository.PermitRepository;
using proyecto_venta_stock.User.UserRepository;
using venta_stock_webapi.Shared.Paged;

namespace proyecto_venta_stock.User.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IPermissionRepository _permitRepository;
        private readonly IMapper _mapper;
        private readonly VentaStockContext _dbContext;
        public UserService(IUserRepository userRepository, ILogger<UserService> logger, IMapper mapper, IPermissionRepository permitRepository, VentaStockContext dbContext)
        {
            _userRepository = userRepository;
            _logger = logger;
            _mapper = mapper;
            _permitRepository = permitRepository;
            _dbContext = dbContext;
        }

        public async Task<Result<bool>> CreateAsync(UserCreateDTO userDTO)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                bool userNameExists = await _userRepository.UserNameInUseAsync(userDTO.Nombre);
                if (userNameExists) return Result<bool>.Failure(UserErrorCode.user_name_in_use);

                bool mailExists = await _userRepository.MailInUseAsync(userDTO.Email);
                if (mailExists) return Result<bool>.Failure(UserErrorCode.user_mail_in_use);

                bool permissions_exists = await _permitRepository.ExistsAsync(userDTO.Permisos);
                if (!permissions_exists) return Result<bool>.Failure(UserErrorCode.permission_not_found);

                var user = _mapper.Map<Usuario>(userDTO);
                user.FechaAlta = DateOnly.FromDateTime(DateTime.Now);

                await _userRepository.CreateAsync(user);

                List<PermisoUsuario> permissionsUser = userDTO.Permisos.Select(id => new PermisoUsuario
                {
                    IdUsuario = user.IdUsuario,
                    IdPermiso = id,
                    FechaAsignacion = DateOnly.FromDateTime(DateTime.Now)
                }).ToList();

                await _permitRepository.AssingPermisionAsync(permissionsUser);

                await transaction.CommitAsync();

                return Result<bool>.Success();
            }
            catch (System.Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure(UserErrorCode.unexpected_error);
            }
        }

        public async Task<Result<bool>> UpdateAsync(UserUpdateDTO userDTO)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                bool exists = await _userRepository.ExistsActive(userDTO.IdUsuario);
                if (!exists) return Result<bool>.Failure(UserErrorCode.user_not_found);

                bool nameInUse = await _userRepository.UserNameInUseAsync(userDTO.IdUsuario, userDTO.Usuario);
                if (nameInUse) return Result<bool>.Failure(UserErrorCode.user_name_in_use);

                bool mailInUse = await _userRepository.MailInUseAsync(userDTO.IdUsuario, userDTO.Email);
                if (mailInUse) return Result<bool>.Failure(UserErrorCode.user_mail_in_use);

                bool permissions_exists = await _permitRepository.ExistsAsync(userDTO.Permisos);

                if (!permissions_exists) return Result<bool>.Failure(UserErrorCode.permission_not_found);

                var user = _mapper.Map<Usuario>(userDTO);

                var row = await _userRepository.UpdateAsync(user);

                if (row == 0)
                {
                    await transaction.RollbackAsync();
                    return Result<bool>.Failure(UserErrorCode.user_not_found);
                }

                var newPermissions = userDTO.Permisos;
                var currentPermissions = await _permitRepository.GetPermissionsUserAsync(user.IdUsuario);

                //Obtengo los permisos nuevos que no esten en los permisos actuales.
                var toAdd = newPermissions.Except(currentPermissions).ToList();
                //Obtengo los permisos viejos que no estan en los permisos actuales.
                var toRemove = currentPermissions.Except(newPermissions).ToList();

                if (toAdd.Any())
                {
                    List<PermisoUsuario> newPermissionsAdd = toAdd.Select(id => new PermisoUsuario
                    {
                        IdUsuario = user.IdUsuario,
                        IdPermiso = id,
                        FechaAsignacion = DateOnly.FromDateTime(DateTime.Now)
                    }).ToList();

                    await _permitRepository.AssingPermisionAsync(newPermissionsAdd);
                }

                if (toRemove.Any()) await _permitRepository.RemovePermissionsAsync(user.IdUsuario, toRemove);

                await transaction.CommitAsync();
                return Result<bool>.Success();
            }
            catch (System.Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure(UserErrorCode.unexpected_error);
            }
        }

        public async Task<Result<bool>> DeleteAsync(int id)
        {
            try
            {
                var exist = await _userRepository.ExistsActive(id);

                if (!exist) return Result<bool>.Failure(UserErrorCode.user_not_found);

                var row = await _userRepository.DeleteAsync(id);

                if (row == 0) return Result<bool>.Failure(UserErrorCode.user_not_found);

                return Result<bool>.Success();
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure(UserErrorCode.unexpected_error);
            }
        }
        public async Task<Result<List<UserDTO>>> GetUsersAsync(int? id)
        {
            try
            {
                var user = await _userRepository.UsersAsync(id);

                if (user is null) return Result<List<UserDTO>>.Failure(UserErrorCode.user_not_found);

                var userDTO = _mapper.Map<List<UserDTO>>(user);

                return Result<List<UserDTO>>.Success(userDTO);
            }
            catch (System.Exception ex)
            {

                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<List<UserDTO>>.Failure(UserErrorCode.unexpected_error);
            }
        }

        public async Task<Result<PagedList<UserDTO>>> UsersPagedAsync(int pageIndex, int pageSize, string searchTerm, string estado = "activos")
        {
            try
            {
                // Base query desde el repositorio
                var query = _userRepository.UsersQueryable(searchTerm);

                // 🔹 Filtro por estado
                if (estado.ToLower() == "activos")
                    query = query.Where(u => u.FechaBaja == null);
                else if (estado.ToLower() == "eliminados")
                    query = query.Where(u => u.FechaBaja != null);

                // 🔹 Proyección
                var projected = _mapper.ProjectTo<UserDTO>(query);

                // 🔹 Paginación
                var paged = await PagedList<UserDTO>.CreateAsync(projected, pageIndex, pageSize);

                return Result<PagedList<UserDTO>>.Success(paged);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado: " + ex);
                return Result<PagedList<UserDTO>>.Failure(UserErrorCode.unexpected_error);
            }
        }


    }
}