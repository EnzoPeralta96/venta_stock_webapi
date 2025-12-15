using System.Security.Claims;
using proyecto_venta_stock.Models;
namespace venta_stock_webapi.Login.JwtService;

public interface IJwtService
{
    (string Token, DateTime Expiration) GenerateJwtToken(Usuario user, IEnumerable<Claim> extraClaims);
}
