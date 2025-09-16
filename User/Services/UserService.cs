using AutoMapper;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Services;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.User.DTO;
using proyecto_venta_stock.User.UserRepository;

namespace proyecto_venta_stock.User.Services
{
    public class UserService : IUserService
    {
        private readonly ILogger<UserService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        public UserService(IUserRepository userRepository, ILogger<UserService> logger, IMapper mapper)
        {
            _userRepository = userRepository;
            _logger = logger;
            _mapper = mapper;
        }
        public async Task<Result<bool>> Create(UserDTO userDTO)
        {
            try
            {
                bool userExists = await _userRepository.Exists(userDTO.Nombre);
                if (userExists) return Result<bool>.Failure("user_name_in_use");

                bool mailExists = await _userRepository.MailInUse(userDTO.Email);
                if (mailExists) return Result<bool>.Failure("user_email_in_use");

                var user = _mapper.Map<Usuario>(userDTO);
                user.FechaAlta = DateOnly.FromDateTime(DateTime.Now);

                await _userRepository.Create(user);

                

                return Result<bool>.Succes();
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado:" + ex.ToString());
                return Result<bool>.Failure("error_inerperado");
            }
        }

    }
}