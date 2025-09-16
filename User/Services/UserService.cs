using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.User.DTO;
using proyecto_venta_stock.User.Repository.PermitRepository;
using proyecto_venta_stock.User.UserRepository;

namespace proyecto_venta_stock.User.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IPermissionRepository _permitRepository;
        private readonly IMapper _mapper;
        public UserService(IUserRepository userRepository, ILogger<UserService> logger, IMapper mapper, IPermissionRepository permitRepository)
        {
            _userRepository = userRepository;
            _logger = logger;
            _mapper = mapper;
            _permitRepository = permitRepository;
        }
        public async Task<Result<bool>> Create(UserDTO userDTO)
        {
            try
            {
                bool userNameExists = await _userRepository.Exists(userDTO.Nombre);
                if (userNameExists) return Result<bool>.Failure("user_name_in_use");

                bool mailExists = await _userRepository.MailInUse(userDTO.Email);
                if (mailExists) return Result<bool>.Failure("user_email_in_use");

                bool permissions_exists = await _permitRepository.Exists(userDTO.Permisos);
                if (!permissions_exists) return Result<bool>.Failure("permit_not_found");

                var user = _mapper.Map<Usuario>(userDTO);
                user.FechaAlta = DateOnly.FromDateTime(DateTime.Now);

                await _userRepository.Create(user);

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

        Task<Result<List<UserDTO>>> IUserService.Users()
        {
            throw new NotImplementedException();
        }
    }
}