using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using proyecto_venta_stock.Models;
using venta_stock_webapi.Shared.JwtBinding;

namespace venta_stock_webapi.Login.JwtService;

public class JwtService : IJwtService
{
    private readonly JwtSettings _jwtSettings;

    public JwtService(IOptions<JwtSettings> options)
    {
        //options.Value es la instancia ya binded con appsettings.json.
        _jwtSettings = options.Value;
    }

    public (string Token, DateTime Expiration) GenerateJwtToken(Usuario user, IEnumerable<Claim> extraClaims)
    {
        var claims = new List<Claim>
          {
              new Claim(JwtRegisteredClaimNames.Sub, user.IdUsuario.ToString()),
              new Claim(JwtRegisteredClaimNames.UniqueName, user.Usuario1),
              new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
          };

        claims.AddRange(extraClaims);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiration = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiration,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return (tokenString, expiration);
    }
}
