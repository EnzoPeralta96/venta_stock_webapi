using AutoMapper;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using proyecto_venta_stock.Data;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.User.DTO;
using proyecto_venta_stock.User.Repository.PermitRepository;
using proyecto_venta_stock.User.UserRepository;
using venta_stock_webapi.Features.Audit.Repository;
using venta_stock_webapi.Features.User.DTO.UserDTO;
using venta_stock_webapi.Shared.Auth.PassService;
using venta_stock_webapi.Shared.Identity;
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
        private readonly IPasswordService _passwordService;
        private readonly IUserContext _userContext;
        private readonly IAuditRepository _auditRepository;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger, IMapper mapper, IPermissionRepository permitRepository, VentaStockContext dbContext, IPasswordService passwordService, IUserContext userContext, IAuditRepository auditRepository)
        {
            _userRepository = userRepository;
            _logger = logger;
            _mapper = mapper;
            _permitRepository = permitRepository;
            _dbContext = dbContext;
            _passwordService = passwordService;
            _userContext = userContext;
            _auditRepository = auditRepository;
        }

        private async Task LogAsync(string accion, string entidadTipo, string detalle,
            object? anterior = null, object? nuevo = null)
        {
            try
            {
                await _auditRepository.LogAsync(new Auditoria
                {
                    FechaHora         = DateTimeOffset.UtcNow,
                    IdUsuario         = _userContext.UserId,
                    UsuarioNombre     = _userContext.UserName,
                    Accion            = accion,
                    EntidadTipo       = entidadTipo,
                    Detalle           = detalle,
                    ValoresAnteriores = anterior != null ? JsonSerializer.Serialize(anterior) : null,
                    ValoresNuevos     = nuevo    != null ? JsonSerializer.Serialize(nuevo)    : null,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "No se pudo registrar auditoría.");
            }
        }

        private async Task<(bool flowControl, Result<bool> value)> ValidateCreate(UserCreateDTO dto)
        {
            if (await _userRepository.UserNameInUseAsync(dto.Nombre))
                return (false, Result<bool>.Failure(UserErrorCode.user_name_in_use));

            if (await _userRepository.MailInUseAsync(dto.Email))
                return (false, Result<bool>.Failure(UserErrorCode.user_mail_in_use));

            if (!await _permitRepository.ExistsAsync(dto.Permisos))
                return (false, Result<bool>.Failure(UserErrorCode.permission_not_found));

            return (true, null);
        }

        private static List<PermisoUsuario> BuildPermissionsForUser(int idUsuario, List<int> permisos)
        {
            return permisos.Select(id => new PermisoUsuario
            {
                IdUsuario       = idUsuario,
                IdPermiso       = id,
                FechaAsignacion = DateOnly.FromDateTime(DateTime.Now)
            }).ToList();
        }

        private async Task LogCreateUserAsync(UserCreateDTO dto)
        {
            await LogAsync("CREACION", "USUARIO",
                $"Usuario creado: '{dto.Usuario}' ({dto.Nombre} {dto.Apellido}) | Rol: {dto.Rol}",
                null,
                new { Usuario = dto.Usuario, Nombre = dto.Nombre, Apellido = dto.Apellido, Rol = dto.Rol, Email = dto.Email });
        }

        public async Task<Result<bool>> CreateAsync(UserCreateDTO userDTO)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                (bool flowControl, Result<bool> value) = await ValidateCreate(userDTO);

                if (!flowControl) return value;

                var user = _mapper.Map<Usuario>(userDTO);
                user.FechaAlta = DateOnly.FromDateTime(DateTime.Now);
                user.Password  = _passwordService.HashPassword(user, userDTO.Password);

                await _userRepository.CreateAsync(user);

                await _permitRepository.AssingPermisionAsync(BuildPermissionsForUser(user.IdUsuario, userDTO.Permisos));

                await transaction.CommitAsync();

                await LogCreateUserAsync(userDTO);

                return Result<bool>.Success();
            }
            catch (System.Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure(UserErrorCode.unexpected_error);
            }
        }



        private async Task<(bool flowControl, Result<bool> value)> ValidateUpdate(UserUpdateDTO dto)
        {
            if (!await _userRepository.ExistsActive(dto.IdUsuario))
                return (false, Result<bool>.Failure(UserErrorCode.user_not_found));

            if (await _userRepository.UserNameInUseAsync(dto.IdUsuario, dto.Usuario))
                return (false, Result<bool>.Failure(UserErrorCode.user_name_in_use));

            if (await _userRepository.MailInUseAsync(dto.IdUsuario, dto.Email))
                return (false, Result<bool>.Failure(UserErrorCode.user_mail_in_use));

            if (!await _permitRepository.ExistsAsync(dto.Permisos))
                return (false, Result<bool>.Failure(UserErrorCode.permission_not_found));

            return (true, null);
        }

        private async Task SyncPermissionsAsync(int idUsuario, List<int> newPermisos)
        {
            var currentPermissions = await _permitRepository.GetPermissionsUserAsync(idUsuario);
            var toAdd    = newPermisos.Except(currentPermissions).ToList();
            var toRemove = currentPermissions.Except(newPermisos).ToList();

            if (toAdd.Any())
                await _permitRepository.AssingPermisionAsync(BuildPermissionsForUser(idUsuario, toAdd));

            if (toRemove.Any())
                await _permitRepository.RemovePermissionsAsync(idUsuario, toRemove);
        }

        private async Task LogUpdateUserAsync(UserUpdateDTO dto)
        {
            await LogAsync("ACTUALIZACION", "USUARIO",
                $"Usuario actualizado: '{dto.Usuario}' ({dto.Nombre} {dto.Apellido}) | Rol: {dto.Rol}",
                null,
                new { Usuario = dto.Usuario, Nombre = dto.Nombre, Apellido = dto.Apellido, Rol = dto.Rol, Email = dto.Email });
        }

        public async Task<Result<bool>> UpdateAsync(UserUpdateDTO userDTO)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                (bool flowControl, Result<bool> value) = await ValidateUpdate(userDTO);
                
                if (!flowControl) return value;

                var user = _mapper.Map<Usuario>(userDTO);
                var row  = await _userRepository.UpdateAsync(user);

                if (row == 0)
                {
                    await transaction.RollbackAsync();
                    return Result<bool>.Failure(UserErrorCode.user_not_found);
                }

                await SyncPermissionsAsync(user.IdUsuario, userDTO.Permisos);
                await transaction.CommitAsync();

                await LogUpdateUserAsync(userDTO);

                return Result<bool>.Success();
            }
            catch (System.Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure(UserErrorCode.unexpected_error);
            }
        }

        public async Task<Result<bool>> ChangePasswordAsync(UserChangePasswordDTO dto)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var user = await _userRepository.GetActiveByIdAsync(dto.IdUsuario);
                if (user is null) return Result<bool>.Failure(UserErrorCode.user_not_found);

                var passwordHashed = _passwordService.HashPassword(user, dto.NewPassword);
                var row = await _userRepository.UpdatePasswordAsync(dto.IdUsuario, passwordHashed);

                if (row == 0)
                {
                    await transaction.RollbackAsync();
                    return Result<bool>.Failure(UserErrorCode.user_not_found);
                }

                await transaction.CommitAsync();

                await LogAsync("CAMBIO_CONTRASEÑA", "USUARIO",
                    $"Contraseña cambiada para: '{user.Usuario1}'");

                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex);
                return Result<bool>.Failure(UserErrorCode.unexpected_error);
            }
        }

        private async Task LogDeleteUserAsync(Usuario user)
        {
            await LogAsync("BAJA", "USUARIO",
                $"Usuario dado de baja: '{user.Usuario1}' ({user.Nombre} {user.Apellido})",
                new { Usuario = user.Usuario1, Nombre = user.Nombre, Apellido = user.Apellido, Activo = true },
                new { Activo = false });
        }

        private async Task LogActivateUserAsync(Usuario user)
        {
            await LogAsync("REACTIVACION", "USUARIO",
                $"Usuario reactivado: '{user.Usuario1}' ({user.Nombre} {user.Apellido})",
                new { Activo = false },
                new { Usuario = user.Usuario1, Nombre = user.Nombre, Apellido = user.Apellido, Activo = true });
        }

        public async Task<Result<bool>> DeleteAsync(int id)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var userToDelete = await _userRepository.GetActiveByIdAsync(id);
                if (userToDelete is null) return Result<bool>.Failure(UserErrorCode.user_not_found);

                var row = await _userRepository.DeleteAsync(id);
                if (row == 0) return Result<bool>.Failure(UserErrorCode.user_not_found);

                await transaction.CommitAsync();
                await LogDeleteUserAsync(userToDelete);

                return Result<bool>.Success();
            }
            catch (System.Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure(UserErrorCode.unexpected_error);
            }
        }

        public async Task<Result<bool>> ActivateAsync(int id)
        {
            using var transaction = await _dbContext.Database.BeginTransactionAsync();
            try
            {
                var userToActivate = await _dbContext.Usuarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.IdUsuario == id);
                if (userToActivate is null) return Result<bool>.Failure(UserErrorCode.user_not_found);

                if (userToActivate.FechaBaja == null) return Result<bool>.Failure(UserErrorCode.user_already_active);

                var row = await _userRepository.ActivateAsync(id);
                if (row == 0)
                {
                    await transaction.RollbackAsync();
                    return Result<bool>.Failure(UserErrorCode.user_not_found);
                }

                await transaction.CommitAsync();
                await LogActivateUserAsync(userToActivate);

                return Result<bool>.Success();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError("Error inesperado:" + ex);
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
                var query = _userRepository.UsersQueryable(searchTerm);

                if (estado.ToLower() == "activos")
                    query = query.Where(u => u.FechaBaja == null);
                else if (estado.ToLower() == "eliminados")
                    query = query.Where(u => u.FechaBaja != null);

                var projected = _mapper.ProjectTo<UserDTO>(query);
                var paged     = await PagedList<UserDTO>.CreateAsync(projected, pageIndex, pageSize);

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