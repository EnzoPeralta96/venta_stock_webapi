# JWT Bearer Authentication en VentaStock (Implementación)

## Objetivo

Implementar **autenticación** (AuthN) basada en **JWT Bearer** para una API consumida por un frontend **React**.

- El frontend obtiene un **Access Token (JWT)** mediante un endpoint de login.
    
- En cada request posterior, el frontend envía el token en el header:
    
    - `Authorization: Bearer <token>`
        
- La API valida el token (firma, expiración, issuer/audience, etc.).
    

> Nota: Este documento cubre **AUTENTICACIÓN (validación de identidad)**. La **AUTORIZACIÓN (permisos/policies)** se documenta por separado.

---

## Componentes

1. **Login Endpoint**
    
    - Recibe credenciales
        
    - Valida usuario + password
        
    - Emite JWT
        
2. **JwtService / TokenService**
    
    - Encapsula la generación del token
        
    - Centraliza configuración (issuer, key, expiración)
        
3. **Configuración JWT Bearer en Program.cs**
    
    - `AddAuthentication().AddJwtBearer(...)`
        
    - `UseAuthentication()` + `UseAuthorization()`
        
4. **Configuración en appsettings.json**
    
    - Key secreta
        
    - Issuer
        
    - Audience
        
    - Expiration
        

---

## Instalar dependencias para JWT : 

```csharp
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
dotnet add package System.IdentityModel.Tokens.Jwt
```
- `Microsoft.AspNetCore.Authentication.JwtBearer`: middleware de autenticación que valida el token en cada request.

    
- `System.IdentityModel.Tokens.Jwt`: utilidades para crear/escribir tokens (JwtSecurityTokenHandler, claims, etc.).
## Configuración (appsettings.json)

Agregar sección:

```json
{
  "Jwt": {
    "Key": "CAMBIAR_ESTA_KEY_POR_UNA_LARGA_Y_SEGURA",//clave simetrica
    "Issuer": "VentaStock", //indica quien emitio el toke
    "Audience": "VentaStock.React", //para quien fue emitido el token
    "ExpiresMinutes": 60
  }
}
```

### Generar Key

- `Key`: mínimo 32+ caracteres (HMAC-SHA256). En producción, usar Secret Manager / variables de entorno.

-  En este caso la key fue generada con la funcion:
  
```SQL Postgres
	--Primero se debe habilitar la extension pgcrypto:
	CREATE EXTENSION IF NOT EXISTS pgcrypto;
	
	-- Genera una Key de 32 bytes -> 256 bits, igual para SHA256
	SELECT encode(gen_random_bytes(32), 'hex');
```

- `Issuer` y `Audience`: valores estables para evitar tokens emitidos por terceros.
    
- `ExpiresMinutes`: access token corto/moderado (ej. 30–120 min).
  
---

## DTOs (Login)

### LoginRequestDTO

```csharp
public class LoginRequestDTO
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
```

### LoginResponseDTO

```csharp
public class LoginResponseDTO
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpirationUtc { get; set; }

    // Datos útiles para el frontend
    public int IdUsuario { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;

    // (Opcional) incluir permisos en respuesta para inicializar UI
    public List<string> Permissions { get; set; } = new();
}
```

---

## JwtSettings (binding de configuración)

**Configuration Binding** es:

> El proceso de **mapear configuración externa**  
> (appsettings.json, variables de entorno, etc.)  
> **a un objeto fuertemente tipado de C#**

En lugar de leer strings “a mano”, dejás que .NET los convierta en un objeto.

```csharp
public class JwtSettings
{
    public string Key { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; }
}
```

Registro en DI (Program.cs):

```csharp
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
```

Dile al contenedor de DI que cuando alguien pida JwtSettings, sus propiedades se llenan automáticamente con los valores del appsettings.json

---

## JwtService (generación del token)

### Interfaz

```csharp
public interface IJwtService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(Usuario user, IEnumerable<Claim> extraClaims);
}
```

### Implementación

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class JwtService : IJwtService
{
    private readonly JwtSettings _settings;

    public JwtService(IOptions<JwtSettings> options)
    {
        _settings = options.Value;
    }

    public (string Token, DateTime ExpiresAtUtc) CreateToken(Usuario user, IEnumerable<Claim> extraClaims)
    {
        // 1) Claims base (identidad)
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.IdUsuario.ToString()),
            new("username", user.Usuario1),
            new(ClaimTypes.Role, user.Rol ?? string.Empty),
        };

        // 2) Claims extra (ej. permisos)
        claims.AddRange(extraClaims);

        // 3) Key + credentials
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // 4) Expiración
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(_settings.ExpiresMinutes);

        // 5) Token
        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: creds
        );

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
```

### Qué hace cada parte

- **Claims**: información que viaja en el token y luego se transforma en `User.Claims`.
    
- **Key/SigningCredentials**: firma HMAC para asegurar que el token no fue alterado.
    
- **Issuer/Audience**: validación adicional.
    
- **Expires**: expiración (la API rechaza tokens vencidos).
    

---

## Configurar JWT Bearer en Program.cs

Agregar:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

//El services AddAuthentication indica el mecanismo: dice algo asi:
//El mecanismo principal de autenticación de esta API es JWT Bearer
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwt["Key"]!);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],

            ValidateAudience = true,
            ValidAudience = jwt["Audience"],

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30) // tolerancia mínima
        };
    });

builder.Services.AddAuthorization();

// Servicios
builder.Services.AddScoped<IJwtService, JwtService>();
```

En el pipeline:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

> Orden correcto: **Authentication antes que Authorization**.

---

## LoginService (uso del JwtService)

Esquema:

1. Buscar usuario por username
    
2. Validar password
    
3. Construir claims extra (permisos)
    
4. Emitir token
    

Ejemplo mínimo (simplificado):

```csharp
public async Task<Result<LoginResponseDTO>> LoginAsync(LoginRequestDTO dto)
{
    var user = await _repo.GetByUsernameAsync(dto.Username);
    if (user == null) return Result<LoginResponseDTO>.Failure(LoginErrorCode.invalid_credentials);

    // Validación de password (hashing documentado aparte)
    if (!VerifyPassword(user, dto.Password))
        return Result<LoginResponseDTO>.Failure(LoginErrorCode.invalid_credentials);

    // Claims extra: permisos (ver doc de autorización)
    var extraClaims = BuildPermissionClaims(user);

    var (token, expiresAt) = _jwtService.CreateToken(user, extraClaims);

    return Result<LoginResponseDTO>.Success(new LoginResponseDTO
    {
        Token = token,
        ExpirationUtc = expiresAt,
        IdUsuario = user.IdUsuario,
        Usuario = user.Usuario1,
        Rol = user.Rol
    });
}
```

---

## Consumo desde React

- Login: guardar token (idealmente en memoria o storage según estrategia).
    
- En requests:
    

```http
Authorization: Bearer <token>
```

- Si la API responde `401 Unauthorized`:
    
    - token inválido o vencido
        
    - el frontend debe redirigir a login / renovar token (si se implementa refresh)
        

---

## Checklist de Validación

-  Existe `Jwt` en `appsettings.json`
    
-  `AddAuthentication().AddJwtBearer(...)` configurado
    
-  `UseAuthentication()` antes de `UseAuthorization()`
    
-  Login endpoint emite token
    
-  Endpoint protegido con `[Authorize]` responde 401 sin token
    
-  Con token válido responde 200
    

---

## Errores típicos

1. **401 siempre**
    
    - Falta `UseAuthentication()`
        
    - Token no se envía como `Bearer`
        
    - Key/issuer/audience no coincide
        
2. **Token válido pero no trae claims**
    
    - No se agregan claims al construir el token
        
3. **ClockSkew demasiado alto**
    
    - Permite tokens vencidos por varios minutos. Mantener bajo.