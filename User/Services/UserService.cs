using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.User.DTO;
using proyecto_venta_stock.User.Repository.PermitRepository;
using proyecto_venta_stock.User.UserRepository;
using venta_stock_webapi.Shared.Paged;
using venta_stock_webapi.User.Services;

namespace proyecto_venta_stock.User.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IPermissionRepository _permitRepository;
        private readonly IMapper _mapper;
        public UserService(IUserRepository userRepository, ILogger<UserService> logger, IMapper mapper, IPermissionRepository permitRepository, IPermissionService permissionService)
        {
            _userRepository = userRepository;
            _logger = logger;
            _mapper = mapper;
            _permitRepository = permitRepository;
        }
        public async Task<Result<bool>> CreateAsync(UserCreateDTO userDTO)
        {
            try
            {
                bool userNameExists = await _userRepository.ExistsAsync(userDTO.Nombre);
                if (userNameExists) return Result<bool>.Failure("user_name_in_use");

                bool mailExists = await _userRepository.MailInUseAsync(userDTO.Email);
                if (mailExists) return Result<bool>.Failure("user_email_in_use");

                bool permissions_exists = await _permitRepository.Exists(userDTO.Permisos);
                if (!permissions_exists) return Result<bool>.Failure("permission_not_found");

                var user = _mapper.Map<Usuario>(userDTO);
                user.FechaAlta = DateOnly.FromDateTime(DateTime.Now);

                await _userRepository.CreateAsync(user);

                List<PermisoUsuario> permissionsUser = userDTO.Permisos.Select(id => new PermisoUsuario
                {
                    IdUsuario = user.IdUsuario,
                    IdPermiso = id,
                    FechaAsignacion = DateOnly.FromDateTime(DateTime.Now)
                }).ToList();

                await _permitRepository.AssingPermision(permissionsUser);

                return Result<bool>.Succes();
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure("error_inerperado");
            }
        }

        public async Task<Result<List<UserDTO>>> GetUsersAsync(int? id)
        {
            try
            {
                var user = await _userRepository.UsersAsync(id);

                if (user is null) return Result<List<UserDTO>>.Failure("user_not_found");

                var userDTO = _mapper.Map<List<UserDTO>>(user);
                
                return Result<List<UserDTO>>.Succes(userDTO);
            }
            catch (System.Exception ex)
            {

                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<List<UserDTO>>.Failure("error_inerperado");
            }
        }

        

        public async Task<Result<PagedList<UserDTO>>> UsersPagedAsync(int pageIndex, int pageSize, string searchTerm)
        {
            try
            {
                var query = _userRepository.UsersQueryable(searchTerm);
                var projected = _mapper.ProjectTo<UserDTO>(query);
                var paged = await PagedList<UserDTO>.CreateAsync(projected, pageSize, pageIndex);
                return Result<PagedList<UserDTO>>.Succes(paged);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<PagedList<UserDTO>>.Failure("error_inerperado");
            }
        }
    }
}