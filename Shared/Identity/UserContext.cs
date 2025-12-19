using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace venta_stock_webapi.Shared.Identity
{
    public class UserContext : IUserContext
    {
        private readonly IHttpContextAccessor _httpContextAccesor;
        public UserContext(IHttpContextAccessor httpContext)
        {
            _httpContextAccesor = httpContext;
        }

        /*
        En caso de que haya un proceso donde no hay un usuario logueado
        Por ejemplo un scafold, el sistema podria lanzar una excepcion.
        Para evitar eso ponemos como usario al admin
        */
        public int UserId
        {
            get
            {
                var context = _httpContextAccesor.HttpContext;

                if (context is null) return 0;

                // ASP.NET Core mapea el claim 'sub' a ClaimTypes.NameIdentifier
                var sub = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                       ?? context.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
                return int.TryParse(sub, out var id) ? id : 0;
            }
        }
        public string UserName
        {
            get
            {
                var context = _httpContextAccesor.HttpContext;
                if (context is null) return null;
                return context.User.FindFirstValue(JwtRegisteredClaimNames.UniqueName) ?? context.User.Identity?.Name;
            }
        }
        public bool IsAuthenticated => _httpContextAccesor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
    }
}