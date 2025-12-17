using System.Security.Claims;
using proyecto_venta_stock.Message;
using proyecto_venta_stock.Models;
using proyecto_venta_stock.Shared.ResultPattern;
using proyecto_venta_stock.User.UserRepository;
using venta_stock_webapi.Login.DTO;
using venta_stock_webapi.Login.JwtService;
using venta_stock_webapi.Shared.Auth;
using venta_stock_webapi.Shared.Auth.PassService;



namespace venta_stock_webapi.Login.Services
{
    public class LoginService : ILoginService
    {
        private readonly ILogger<LoginService> _logger;
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly IPasswordService _passwordService;

        public LoginService(IUserRepository userRepository, IJwtService jwtService, ILogger<LoginService> logger, IPasswordService passwordService)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _logger = logger;
            _passwordService = passwordService;
        }

        private IEnumerable<Claim> BuildPermissionsUserClaims(Usuario user)
        {
            return user.PermisoUsuarios.Select(pu => pu.IdPermisoNavigation.Permiso1)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(p => new Claim(AuthConstants.PermissionClaimType, p!))
                .ToList();
        }

        public async Task<Result<LoginResponseDTO>> AuthenticateAsync(LoginRequestDTO loginRequest)
        {
            try
            {
                var user = await _userRepository.GetByUserNameAsync(loginRequest.Username);

                if (user == null) return Result<LoginResponseDTO>.Failure(UserErrorCode.username_not_found);
                

                if(!_passwordService.VerifyPassword(user, loginRequest.Password)) return Result<LoginResponseDTO>.Failure(UserErrorCode.username_not_found);

                
                var permissionClaims = BuildPermissionsUserClaims(user);
                var (token, expiration) = _jwtService.GenerateJwtToken(user, permissionClaims);

                var LoginResponseDTO = new LoginResponseDTO
                {
                    Token = token,
                    Expiration = expiration,
                    UserId = user.IdUsuario,
                    Username = user.Nombre + " " + user.Apellido,
                    Role = user.Rol,
                    Permissions = permissionClaims.Select(c => c.Value).ToList()
                };

                return Result<LoginResponseDTO>.Success(LoginResponseDTO);
            }
            catch (System.Exception ex)
            {
                _logger.LogError("Error inesperado durante la autenticación: " + ex.ToString());
                return Result<LoginResponseDTO>.Failure(UserErrorCode.unexpected_error);
            }
        }

       
    }
}