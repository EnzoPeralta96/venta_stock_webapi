
## Descripción General

  

El módulo de **Login & Authorization** gestiona la autenticación y autorización de usuarios mediante **JWT (JSON Web Tokens)** con permisos granulares incluidos como claims. Incluye:

- **Autenticación (AuthN)**: Validación de credenciales con contraseñas hasheadas (ASP.NET Identity)

- **Autorización (AuthZ)**: Sistema de permisos dinámicos basado en policies

- **Logout**: Cierre de sesión del lado del cliente

**Estado**: ✅ **COMPLETO** (100%)

---
## Arquitectura del Módulo

### Estructura de Carpetas
  
```

Login/

├── Controllers/

│   ├── LoginController.cs                  # Endpoint de autenticación

├── Services/

│   ├── LoginServices/

│   │   ├── LoginService.cs                 # Lógica de autenticación

│   │   └── ILoginService.cs                # Contrato del servicio

│   └── JwtService/

│       ├── JwtService.cs                   # Generación de JWT

│       └── IJwtService.cs                  # Contrato del servicio

└── DTO/

    ├── LoginRequestDTO.cs                  # DTO de entrada

    └── LoginResponseDTO.cs                 # DTO de salida

  

Shared/Auth/

├── AuthConstants.cs                        # Constantes de autenticación

├── PassService/

│   ├── PasswordService.cs                  # Hasheo y verificación

│   └── IPasswordService.cs                 # Contrato del servicio

└── Authorization/

    ├── PermissionRequirement.cs            # Requirement de permisos

    ├── PermissionHandler.cs                # Handler de permisos

    └── PermissionPolicyProvider.cs         # Provider de policies dinámicas

  

Shared/JwtBinding/

└── JwtSettings.cs                          # Binding de configuración JWT

```

---

# PARTE 1: AUTENTICACIÓN (Authentication)

## Validaciones de Seguridad

### 1. Validaciones de Entrada (DTO)

El sistema valida los datos de entrada antes de procesar la autenticación:


- **Username**: Campo obligatorio con `[Required]`

- **Password**: Campo obligatorio con `[Required]`


Si alguna validación falla, el endpoint retorna `400 Bad Request` con el mensaje de error correspondiente.

### 2. Validación de Usuarios Activos

Solo usuarios activos pueden autenticarse:

```csharp

// UserRepository.cs:161

return _dbContext.Usuarios

    .Where(u => u.Usuario1 == userName && u.FechaBaja == null)  // ✅ Solo usuarios activos

    .Include(u => u.PermisoUsuarios)

        .ThenInclude(pu => pu.IdPermisoNavigation)

    .FirstOrDefaultAsync();

```

**Seguridad**: Los usuarios con `FechaBaja != null` (eliminados lógicamente) no pueden autenticarse, incluso si conocen la contraseña correcta.

---
## Flujo de Autenticación

### 1. Usuario envía credenciales

```http

POST /api/Login

{

  "username": "admin",

  "password": "Admin123!"

}

```

### 2. LoginService valida credenciales

```csharp

// LoginService.cs:43-48

var user = await _userRepository.GetByUserNameAsync(loginRequest.Username);

if (user == null) return Result<LoginResponseDTO>.Failure(UserErrorCode.username_not_found);

  

if(!_passwordService.VerifyPassword(user, loginRequest.Password))

    return Result<LoginResponseDTO>.Failure(UserErrorCode.username_not_found);

```

### 3. Se construyen los claims de permisos

```csharp

// LoginService.cs:30-37

private IEnumerable<Claim> BuildPermissionsUserClaims(Usuario user)

{

    return user.PermisoUsuarios.Select(pu => pu.IdPermisoNavigation.Permiso1)

        .Where(p => !string.IsNullOrWhiteSpace(p))

        .Distinct(StringComparer.OrdinalIgnoreCase)

        .Select(p => new Claim(AuthConstants.PermissionClaimType, p!))

        .ToList();

}

```


### 4. JwtService genera el token

```csharp

// JwtService.cs:21-48

var claims = new List<Claim>

{

    new Claim(JwtRegisteredClaimNames.Sub, user.IdUsuario.ToString()),

    new Claim(JwtRegisteredClaimNames.Email, user.Email),

    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())

};

claims.AddRange(extraClaims); // Permisos del usuario

  

var token = new JwtSecurityToken(

    issuer: _jwtSettings.Issuer,

    audience: _jwtSettings.Audience,

    claims: claims,

    expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes),

    signingCredentials: creds

);

```

### 5. Se retorna el LoginResponseDTO

```json

{

  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",

  "expiration": "2025-12-15T00:10:50.4719084Z",

  "userId": 1,

  "username": "admin",

  "role": "admin",

  "permissions": [

    "USR_CREATE",

    "USR_READ",

    "VEN_CREATE",

    "PROD_UPDATE",

    ...

  ]

}

```

---
## DTOs (Data Transfer Objects)

### LoginRequestDTO (Entrada)

```csharp

public class LoginRequestDTO

{

    [Required (ErrorMessage = "El nombre de usuario es obligatorio.")]

    public string Username { get; set; }

  

    [Required (ErrorMessage = "La contraseña es obligatoria.")]

    public string Password { get; set; }

}

```


**Validaciones**:

- `Username`: Obligatorio (campo requerido)

- `Password`: Obligatorio (campo requerido)


**Respuesta de Error de Validación** (400 Bad Request):

```json

{

  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",

  "title": "One or more validation errors occurred.",

  "status": 400,

  "errors": {

    "Username": ["El nombre de usuario es obligatorio."],

    "Password": ["La contraseña es obligatoria."]

  }

}

```

### LoginResponseDTO (Salida)


```csharp

public class LoginResponseDTO

{

    public string Token { get; set; }              // JWT token

    public DateTime Expiration { get; set; }       // Fecha de expiración

  

    public int UserId { get; set; }

    public string Username { get; set; }

    public string Role { get; set; }               // Rol del usuario

  

    public List<string> Permissions { get; set; }  // Lista de permisos

}

```

  
**Nota**: Los permisos se retornan en formato legible (ej: `"USR_CREATE"`) y también están embebidos en el JWT como claims.

---
## Endpoints REST - Autenticación

### LoginController


Base URL: `/api/Login`

#### POST /api/Login - Autenticar Usuario

  

```http

POST /api/Login

Content-Type: application/json

{

  "username": "admin",

  "password": "Admin123!"

}

```


**Respuesta Exitosa** (200 OK):

```json

{

  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJhZG1pbkBhZG1pbiIsImp0aSI6IjAyNjg3ODU5LTQ1OWUtNGEwYi04YThmLThkNDAxNDYxZjk0MSIsInBlcm1pc3Npb25zIjpbIlVTUl9DUkVBVEUiLCJVU1JfUkVBRCJdLCJleHAiOjE3NjU3NTc0NTAsImlzcyI6IllvdXJBcHBJc3N1ZXIiLCJhdWQiOiJZb3VyQXBwQXVkaWVuY2UifQ...",

  "expiration": "2025-12-15T00:10:50.4719084Z",

  "userId": 1,

  "username": "admin",

  "role": "admin",

  "permissions": [

    "USR_CREATE",

    "USR_READ",

    "USR_UPDATE",

    "USR_DELETE",

    "VEN_CREATE",

    "PROD_UPDATE",

    ...

  ]

}

```


**Errores**:

- `400 Bad Request`:

  - Validación fallida (username o password faltantes)

  - Credenciales inválidas (username o password incorrectos)

- `500 Internal Server Error`: Error inesperado

**Códigos de Error**:

```csharp

// Validaciones de entrada

BadRequest(ModelState)            // Campos requeridos faltantes

  

// Lógica de autenticación

UserErrorCode.username_not_found  // Usuario no existe o contraseña incorrecta

UserErrorCode.unexpected_error    // Error inesperado

```

---
## Servicios de Autenticación

### LoginService

#### Método: `AuthenticateAsync(LoginRequestDTO loginRequest)`

  

**Responsabilidad**: Autenticar usuario y generar JWT con permisos.

  

**Flujo**:

1. **Buscar usuario por username**:

   ```csharp

   var user = await _userRepository.GetByUserNameAsync(loginRequest.Username);

   if (user == null) return Result<LoginResponseDTO>.Failure(UserErrorCode.username_not_found);

   ```

  

2. **Verificar contraseña hasheada**:

   ```csharp

   if(!_passwordService.VerifyPassword(user, loginRequest.Password))

       return Result<LoginResponseDTO>.Failure(UserErrorCode.username_not_found);

   ```

  

3. **Construir claims de permisos**:

   ```csharp

   var permissionClaims = BuildPermissionsUserClaims(user);

   ```

  

4. **Generar JWT**:

   ```csharp

   var (token, expiration) = _jwtService.GenerateJwtToken(user, permissionClaims);

   ```

  

5. **Retornar LoginResponseDTO**:

   ```csharp

   return Result<LoginResponseDTO>.Success(new LoginResponseDTO

   {

       Token = token,

       Expiration = expiration,

       UserId = user.IdUsuario,

       Username = user.Usuario1,

       Role = user.Rol,

       Permissions = permissionClaims.Select(c => c.Value).ToList()

   });

   ```

  

**Nota de Seguridad**: Por motivos de seguridad, se retorna el mismo error (`username_not_found`) tanto si el usuario no existe como si la contraseña es incorrecta. Esto evita revelar qué usuarios existen en el sistema.

  

---

  

### JwtService

  

#### Método: `GenerateJwtToken(Usuario user, IEnumerable<Claim> extraClaims)`

  

**Responsabilidad**: Generar token JWT firmado con claims de usuario y permisos.

  

**Claims Estándar**:

- `sub` (Subject): ID del usuario

- `email`: Email del usuario

- `jti` (JWT ID): GUID único del token

  

**Claims Extra (Permisos)**:

- `permissions`: Array de permisos (ej: `["USR_CREATE", "VEN_CREATE"]`)

  

**Configuración JWT**:

```csharp

var token = new JwtSecurityToken(

    issuer: "YourAppIssuer",           // Emisor del token

    audience: "YourAppAudience",       // Audiencia del token

    claims: claims,                     // Claims combinados

    expires: DateTime.UtcNow.AddMinutes(60),  // Expira en 60 minutos

    signingCredentials: creds           // Firma HMAC-SHA256

);

```

  

**Retorno**: Tupla `(string Token, DateTime Expiration)`

  

---

  

### PasswordService

  

#### Método: `HashPassword(Usuario user, string plainPassword)`

  

**Responsabilidad**: Hashear contraseña usando ASP.NET Identity.

  

```csharp

public string HashPassword(Usuario user, string plainPassword)

    => _hasher.HashPassword(user, plainPassword);

```

  

**Output**: String hasheado (ej: `"AQAAAAIAAYagAAAAEH..."`)

  

---

  

#### Método: `VerifyPassword(Usuario user, string providedPassword)`

  

**Responsabilidad**: Verificar contraseña hasheada vs texto plano.

  

```csharp

public bool VerifyPassword(Usuario user, string providedPassword)

{

    var result = _hasher.VerifyHashedPassword(

        user,

        user.Password,      // Hash almacenado en DB

        providedPassword    // Contraseña en texto plano del login

    );

    return result != PasswordVerificationResult.Failed;

}

```

  

**Retorno**: `true` si la contraseña coincide, `false` si no.

  

---

  

## Configuración JWT

### appsettings.json


```json

{

  "Jwt": {

    "Key": "733b5cb0d6d61a685237f37e950f1b3b7f8ece4a4259e8e462366a34d98e49e0",

    "Issuer": "YourAppIssuer",

    "Audience": "YourAppAudience",

    "ExpireMinutes": 60

  }

}

```

  

**Configuración**:

- `Key`: Clave secreta de 256 bits (64 caracteres hex) para firmar el token

- `Issuer`: Identificador del emisor del token

- `Audience`: Identificador de la audiencia (aplicación que consume el token)

- `ExpireMinutes`: Tiempo de vida del token en minutos (default: 60)

---
### Program.cs - Configuración de Autenticación

#### 1. Binding de JwtSettings

```csharp

// Program.cs:68-69

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));

builder.Services.AddScoped<IJwtService, JwtService>();

```

#### 2. Configuración de JWT Bearer

```csharp

// Program.cs:71-92

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)

    .AddJwtBearer(options =>

    {

        var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>();

        var keyBytes = Encoding.UTF8.GetBytes(jwtSettings.Key);

  

        options.TokenValidationParameters = new TokenValidationParameters

        {

            ValidateIssuerSigningKey = true,

            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

  

            ValidateIssuer = true,

            ValidIssuer = jwtSettings.Issuer,

  

            ValidateAudience = true,

            ValidAudience = jwtSettings.Audience,

  

            ValidateLifetime = true,

            ClockSkew = TimeSpan.FromSeconds(30)  // Tolerancia de 30 segundos

        };

    });

```

#### 3. Configuración de Autorización

```csharp

// Program.cs:94-96

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();

builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();

```

#### 4. Registro de Servicios de Autenticación

```csharp

// Program.cs:117-122

builder.Services.AddScoped<ILoginService, LoginService>();

builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services.AddScoped<IPasswordService, PasswordService>();

```

#### 5. Middleware de Autenticación/Autorización

```csharp

// Program.cs:141-142

app.UseAuthentication();  // ✅ Debe ir ANTES de UseAuthorization

app.UseAuthorization();

```


**Orden de Middlewares** (importante):

1. `UseHttpsRedirection()`

2. `UseCors()`

3. `UseAuthentication()` ← Lee el token y construye el ClaimsPrincipal

4. `UseAuthorization()` ← Valida permisos

5. `MapControllers()`

  
---
# PARTE 2: AUTORIZACIÓN (Authorization)

## Sistema de Permisos Dinámicos

### Estructura de Claims en JWT

Los permisos se incluyen como claims con el tipo `"permissions"`:


```json

{

  "sub": "1",

  "email": "admin@admin",

  "jti": "02687859-459e-4a0b-8a8f-8d401461f941",

  "permissions": [

    "USR_CREATE",

    "USR_READ",

    "USR_UPDATE",

    "USR_DELETE",

    "VEN_CREATE",

    "PROD_UPDATE",

    ...

  ],

  "exp": 1765757450,

  "iss": "YourAppIssuer",

  "aud": "YourAppAudience"

}

```

### AuthConstants

```csharp

// Shared/Auth/AuthConstants.cs

public static class AuthConstants

{

    public const string PermissionClaimType = "permissions";

    public const string PermissionPolicyPrefix = "PERM:";

}

```


**Uso**:

- `PermissionClaimType`: Tipo de claim para permisos en el JWT

- `PermissionPolicyPrefix`: Prefijo para policies dinámicas (ej: `"PERM:USR_CREATE"`)


---

## Componentes de Autorización
  
### 1. PermissionRequirement


**Ubicación**: `Shared/Auth/Authorization/PermissionRequirement.cs`
  

```csharp

public class PermissionRequirement : IAuthorizationRequirement

{

    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

}

```


**Propósito**: Define el requirement (requisito) de tener un permiso específico.

---
### 2. PermissionHandler

**Ubicación**: `Shared/Auth/Authorization/PermissionHandler.cs`

```csharp

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>

{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,

        PermissionRequirement requirement)
    {
        // 1. Validar que el usuario esté autenticado

        var userIsNotLogged = context.User?.Identity?.IsAuthenticated != true;

        if (userIsNotLogged) return Task.CompletedTask;
  
        // 2. Verificar que tenga el claim de permiso requerido

        var hasPermissionClaim = context.User.Claims.Any(c =>

            c.Type == AuthConstants.PermissionClaimType &&

            string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        // 3. Si tiene el permiso, marcar como exitoso

        if (hasPermissionClaim) context.Succeed(requirement);

        return Task.CompletedTask;

    }

}

```

**Propósito**: Verifica si el usuario autenticado tiene el permiso requerido en sus claims.

**Flujo**:

1. Valida que el usuario esté autenticado

2. Busca el claim `permissions` con el valor del permiso requerido

3. Si lo encuentra, marca el requirement como cumplido con `context.Succeed()`

---
### 3. PermissionPolicyProvider

**Ubicación**: `Shared/Auth/Authorization/PermissionPolicyProvider.cs`

```csharp

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider

{

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
    {

    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)

    {

        // 1. Detectar si la policy empieza con "PERM:"

        if (policyName.StartsWith(AuthConstants.PermissionPolicyPrefix, StringComparison.OrdinalIgnoreCase))

        {

            // 2. Extraer el permiso del nombre de la policy

            var permission = policyName.Substring(AuthConstants.PermissionPolicyPrefix.Length).Trim();

  

            if (string.IsNullOrWhiteSpace(permission))

                return Task.FromResult<AuthorizationPolicy?>(null);

  

            // 3. Construir una AuthorizationPolicy en runtime

            var policy = new AuthorizationPolicyBuilder()

                .RequireAuthenticatedUser()

                .AddRequirements(new PermissionRequirement(permission))

                .Build();

  

            return Task.FromResult<AuthorizationPolicy?>(policy);

        }

  

        // 4. Si no es PERM:, delegar al provider base

        return base.GetPolicyAsync(policyName);

    }

}

```

**Propósito**: Genera policies dinámicamente en runtime basadas en el prefijo `PERM:`.

**Flujo**:

1. ASP.NET solicita una policy (ej: `"PERM:USR_CREATE"`)

2. El provider detecta el prefijo `PERM:`

3. Extrae el permiso requerido: `"USR_CREATE"`

4. Construye una policy que requiere usuario autenticado + el permiso

5. Retorna la policy generada

**Beneficio**: No hace falta registrar manualmente 31+ políticas en `Program.cs`.

---
## Uso en Controllers

### Ejemplo Básico

```csharp

using Microsoft.AspNetCore.Authorization;

  

[ApiController]

[Route("api/[controller]")]

public class UserController : ControllerBase

{
    [Authorize(Policy = "PERM:USR_CREATE")]

    [HttpPost("create")]

    public async Task<IActionResult> CreateUser(UserCreateDTO dto)

    {

        // Solo usuarios con el permiso USR_CREATE pueden acceder

        // ...

    }

    [Authorize(Policy = "PERM:USR_READ")]

    [HttpGet("users")]

    public async Task<IActionResult> GetUsers()

    {

        // Solo usuarios con el permiso USR_READ pueden acceder

        // ...
    }


    [Authorize(Policy = "PERM:USR_UPDATE")]

    [HttpPut("update")]

    public async Task<IActionResult> UpdateUser(UserUpdateDTO dto)

    {

        // Solo usuarios con el permiso USR_UPDATE pueden acceder

        // ...

    }

  

    [Authorize(Policy = "PERM:USR_DELETE")]

    [HttpDelete("delete/{id}")]

    public async Task<IActionResult> DeleteUser(int id)

    {

        // Solo usuarios con el permiso USR_DELETE pueden acceder

        // ...

    }

}

```

### Ejemplo Múltiples Permisos


```csharp

// Requiere AMBOS permisos (AND)

[Authorize(Policy = "PERM:VEN_CREATE")]

[Authorize(Policy = "PERM:VEN_NO_STOCK")]

[HttpPost("sales/without-stock")]

public async Task<IActionResult> CreateSaleWithoutStock() { ... }

```


### Controller Completo


```csharp

[ApiController]

[Route("api/[controller]")]

[Authorize]  // ← Todos los endpoints requieren autenticación

public class ProductController : ControllerBase

{

    [Authorize(Policy = "PERM:PROD_CREATE")]

    [HttpPost("create")]

    public async Task<IActionResult> CreateProduct(...) { ... }

  

    [Authorize(Policy = "PERM:PROD_READ")]

    [HttpGet("products")]

    public async Task<IActionResult> GetProducts() { ... }

  

    [Authorize(Policy = "PERM:PROD_UPDATE")]

    [HttpPut("update")]

    public async Task<IActionResult> UpdateProduct(...) { ... }

  

    [Authorize(Policy = "PERM:PROD_PRICE_UPDATE")]

    [HttpPut("update-price")]

    public async Task<IActionResult> UpdatePrice(...) { ... }

  

    [Authorize(Policy = "PERM:PROD_DELETE")]

    [HttpDelete("delete/{id}")]

    public async Task<IActionResult> DeleteProduct(int id) { ... }

}

```

---

## Flujo Completo de Autorización

### Escenario: Usuario intenta crear un producto


1. **Frontend envía petición**:

   ```http

   POST /api/Product/create

   Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

   ```

  

2. **Middleware de Autenticación** (`UseAuthentication()`):

   - Extrae el token del header

   - Valida firma, issuer, audience, lifetime

   - Construye `ClaimsPrincipal` con los claims del token

   - Asigna a `HttpContext.User`

  

3. **Middleware de Autorización** (`UseAuthorization()`):

   - Detecta `[Authorize(Policy = "PERM:PROD_CREATE")]`

   - Llama a `PermissionPolicyProvider.GetPolicyAsync("PERM:PROD_CREATE")`

  

4. **PermissionPolicyProvider**:

   - Detecta prefijo `PERM:`

   - Extrae permiso: `"PROD_CREATE"`

   - Genera policy con `PermissionRequirement("PROD_CREATE")`

  

5. **PermissionHandler**:

   - Verifica `User.Claims` buscando `{ Type: "permissions", Value: "PROD_CREATE" }`

   - Si existe → `context.Succeed(requirement)`

   - Si no existe → requirement falla

  

6. **Resultado**:

   - ✅ Si tiene el permiso → 200 OK (ejecuta el endpoint)

   - ❌ Si no tiene el permiso → 403 Forbidden

   - ❌ Si no está autenticado → 401 Unauthorized

  

---

  

## Códigos HTTP de Autorización

  

- **200 OK**: Petición exitosa, usuario tiene permisos

- **401 Unauthorized**: Token inválido, expirado o ausente

- **403 Forbidden**: Token válido pero usuario no tiene el permiso requerido

- **500 Internal Server Error**: Error inesperado

  

---

  

## Ejemplo de Permisos por Rol

  

### Administrador Principal

```json

{

  "role": "admin",

  "permissions": [

    "USR_CREATE", "USR_READ", "USR_UPDATE", "USR_DELETE", "USR_ROLE_ASSIGN",

    "REP_GENERATE", "REP_EXPORT",

    "HIS_VIEW",

    "VEN_CREATE", "VEN_INVOICE", "VEN_NO_STOCK", "VEN_AUTH_OVERLIMIT",

    "CC_VIEW", "CC_NOTE_DEBIT", "CC_NOTE_CREDIT",

    "PROD_CREATE", "PROD_READ", "PROD_UPDATE", "PROD_DELETE", "PROD_BARCODE",

    "PROD_PRICE_UPDATE", "PROD_STOCK_LOW", "PROD_STOCK_IN",

    "CLI_CREATE", "CLI_READ", "CLI_UPDATE", "CLI_DELETE",

    "SEARCH_USER", "SEARCH_CLIENT", "SEARCH_PRODUCT", "SEARCH_SALE"

  ]

}

```

  

### Vendedor

```json

{

  "role": "vendedor",

  "permissions": [

    "VEN_CREATE", "VEN_INVOICE",

    "CLI_CREATE", "CLI_READ", "CLI_UPDATE",

    "PROD_READ",

    "SEARCH_CLIENT", "SEARCH_PRODUCT", "SEARCH_SALE"

  ]

}

```

### Encargado de Precios

```json

{

  "role": "encargado_precios",

  "permissions": [

    "PROD_CREATE", "PROD_READ", "PROD_UPDATE", "PROD_DELETE",

    "PROD_BARCODE", "PROD_PRICE_UPDATE", "PROD_STOCK_LOW", "PROD_STOCK_IN",

    "SEARCH_PRODUCT"

  ]

}

```


---

## Debug y Troubleshooting

### Ver permisos del token en un endpoint

  

```csharp

[HttpGet("debug-permissions")]

[Authorize]

public IActionResult DebugPermissions()

{

    var permissions = User.Claims

        .Where(c => c.Type == AuthConstants.PermissionClaimType)

        .Select(c => c.Value)

        .ToList();

  

    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    var email = User.FindFirst(ClaimTypes.Email)?.Value;

  

    return Ok(new

    {

        UserId = userId,

        Email = email,

        Permissions = permissions

    });

}

```

### Errores Comunes

  

**1. Siempre 403 Forbidden**

- ✅ Verificar que los claims `permissions` se agreguen al token en el login

- ✅ Verificar que el nombre del permiso coincida (case-insensitive)

- ✅ Verificar que el prefijo sea `PERM:` en el atributo `[Authorize]`

**2. Siempre 401 Unauthorized**

- ✅ Verificar que `UseAuthentication()` esté en el pipeline

- ✅ Verificar que el token se envíe en el header: `Authorization: Bearer <token>`

- ✅ Verificar configuración de issuer/audience/key en `appsettings.json`

  

**3. Claims no cargados en el token**

- ✅ Verificar que `LoginService` incluya `PermisoUsuarios` con `.Include()`

- ✅ Verificar que `BuildPermissionsUserClaims()` se ejecute

- ✅ Verificar que los claims se agreguen con `claims.AddRange(permissionClaims)`


---
## Seguridad

  

### 1. Contraseñas Hasheadas

- ✅ Usa `PasswordHasher<Usuario>` de ASP.NET Identity

- ✅ Algoritmo: PBKDF2 con salt aleatorio

- ✅ Formato: `AQAAAAxxx...` (base64)

- ❌ **NUNCA** almacenar contraseñas en texto plano

  

### 2. Token JWT Firmado

- ✅ Algoritmo: HMAC-SHA256

- ✅ Clave de 256 bits

- ✅ Validación de firma, issuer, audience y lifetime

- ⚠️ La clave secreta debe estar en variables de entorno en producción

### 3. Validación de Credenciales

- ✅ Mensaje de error genérico (no revela si usuario existe)

- ✅ Verificación de contraseña en tiempo constante

- ✅ Log de intentos fallidos (para auditoría)

### 4. Expiración de Tokens

- ✅ Tokens expiran en 60 minutos por defecto

- ✅ ClockSkew de 30 segundos (tolerancia de reloj)

- ✅ Validación de lifetime habilitada

### 5. Autorización Granular

- ✅ Permisos específicos por operación

- ✅ Usuarios no atados a roles fijos

- ✅ Policies dinámicas escalables

  
---

## Testing (Sugerencias)

### Tests Unitarios - LoginService


```csharp

[Fact]

public async Task AuthenticateAsync_WithValidCredentials_ReturnsToken()

{

    // Arrange

    var loginRequest = new LoginRequestDTO

    {

        Username = "admin",

        Password = "Admin123!"

    };

  

    var user = new Usuario

    {

        IdUsuario = 1,

        Usuario1 = "admin",

        Password = "hashedPassword",

        PermisoUsuarios = new List<PermisoUsuario>()

    };

  

    _mockUserRepo.Setup(r => r.GetByUserNameAsync("admin"))

        .ReturnsAsync(user);

    _mockPasswordService.Setup(s => s.VerifyPassword(user, "Admin123!"))

        .Returns(true);

  

    // Act

    var result = await _service.AuthenticateAsync(loginRequest);

  

    // Assert

    Assert.True(result.IsSuccess);

    Assert.NotNull(result.Value.Token);

    Assert.Equal(1, result.Value.UserId);

}

  

[Fact]

public async Task AuthenticateAsync_WithInvalidPassword_ReturnsFailure()

{

    // Arrange

    var loginRequest = new LoginRequestDTO

    {

        Username = "admin",

        Password = "WrongPassword"

    };

  

    var user = new Usuario { Password = "hashedPassword" };

  

    _mockUserRepo.Setup(r => r.GetByUserNameAsync("admin"))

        .ReturnsAsync(user);

    _mockPasswordService.Setup(s => s.VerifyPassword(user, "WrongPassword"))

        .Returns(false);

  

    // Act

    var result = await _service.AuthenticateAsync(loginRequest);

  

    // Assert

    Assert.False(result.IsSuccess);

    Assert.Equal(UserErrorCode.username_not_found, result.ErrorCode);

}

```

### Tests de Integración - Autorización

```csharp

[Fact]

public async Task CreateUser_WithoutPermission_Returns403()

{

    // Arrange

    var token = GenerateTokenWithoutPermission("USR_CREATE");

    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

  

    // Act

    var response = await _client.PostAsJsonAsync("/api/User/create", new UserCreateDTO());

  

    // Assert

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);

}

  

[Fact]

public async Task CreateUser_WithPermission_Returns200()

{

    // Arrange

    var token = GenerateTokenWithPermission("USR_CREATE");

    _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

  

    // Act

    var response = await _client.PostAsJsonAsync("/api/User/create", validUserDto);

  

    // Assert

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

}

```

--- 
