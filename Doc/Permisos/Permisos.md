# Autorización por Permisos con Policies Dinámicas (PERM:)

## Objetivo

Implementar **autorización** (AuthZ) basada en **permisos** usando:

- Claims en JWT: `permission = <CODIGO>`
    
- Policies **dinámicas**: `[Authorize(Policy = "PERM:USR_CREATE")]`
    

Esto permite escalar sin declarar manualmente una policy por cada permiso.

---

## Conceptos

### Autenticación vs Autorización

- **Autenticación (AuthN)**: ¿Quién es el usuario?
    
    - Resuelta por JWT Bearer (token válido)
        
- **Autorización (AuthZ)**: ¿Qué puede hacer?
    
    - Resuelta por claims + policies
        

---

## Convenciones Adoptadas

### Claim de permisos

- **Type**: `permission`
    
- **Value**: código del permiso (ej. `USR_CREATE`, `VEN_CREATE`)
    

Se agregan múltiples claims con el mismo type:

```csharp
new Claim("permission", "USR_CREATE")
new Claim("permission", "USR_READ")
new Claim("permission", "VEN_CREATE")
```

### Policy dinámica

- Prefijo: `PERM:`
    
- Ejemplo:
    
    - `PERM:USR_CREATE`
        
    - `PERM:VEN_CREATE`
        

El nombre de la policy **codifica el permiso requerido**.

---

## Constantes (recomendado)

```csharp
public static class AuthConstants
{
    public const string PermissionClaimType = "permission";
    public const string PermissionPolicyPrefix = "PERM:";
}
```

**Por qué:**

- Evita strings hardcodeados en múltiples archivos
    
- Reduce errores tipográficos
    

---

## Parte 1: PermissionRequirement

### Código

```csharp
using Microsoft.AspNetCore.Authorization;

public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
```

### Explicación

- `IAuthorizationRequirement` representa **una condición** que debe cumplirse.
    
- En este caso, la condición es: _"el usuario debe tener el permiso X"_.
    
- `Permission` almacena el permiso requerido (ej. `USR_CREATE`).
    

---

## Parte 2: PermissionHandler

### Código

```csharp
using Microsoft.AspNetCore.Authorization;

public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // 1) Validación mínima: usuario autenticado
        if (context.User?.Identity?.IsAuthenticated != true)
            return Task.CompletedTask;

        // 2) Verificación: existe claim permission con el valor requerido
        var hasPermission = context.User.Claims.Any(c =>
            c.Type == AuthConstants.PermissionClaimType &&
            string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        // 3) Si cumple, se marca como exitoso
        if (hasPermission)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
```

### Explicación por pasos

1. **Autenticación previa**: aunque la policy exige autenticación, se valida por seguridad.
    
2. **Búsqueda de claim**:
    
    - Recorre `User.Claims`
        
    - Busca `Type == "permission"` y `Value == <permiso>`
        
3. **`context.Succeed(requirement)`**:
    
    - Marca el requirement como cumplido
        
    - La autorización se considera aprobada
        

---

## Parte 3: PermissionPolicyProvider (policy dinámica)

### Código

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

public sealed class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        // 1) Intercepta policies con el prefijo PERM:
        if (policyName.StartsWith(AuthConstants.PermissionPolicyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            // 2) Extrae el permiso del nombre de policy
            var permission = policyName.Substring(AuthConstants.PermissionPolicyPrefix.Length).Trim();

            if (string.IsNullOrWhiteSpace(permission))
                return Task.FromResult<AuthorizationPolicy?>(null);

            // 3) Construye una AuthorizationPolicy en runtime
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();

            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        // 4) Si no es PERM:, delega al provider base
        return base.GetPolicyAsync(policyName);
    }
}
```

### Explicación por pasos

1. **Detecta** si la policy solicitada por `[Authorize(Policy = "...")]` empieza con `PERM:`.
    
2. **Extrae** el permiso requerido desde el nombre.
    
3. **Construye** una policy en runtime:
    
    - Requiere usuario autenticado
        
    - Agrega `PermissionRequirement(permission)`
        
4. Si no coincide con `PERM:`, se comporta como el provider normal.
    

**Beneficio principal:** no hace falta registrar 100 policies manualmente.

---

## Parte 4: Registrar en DI (Program.cs)

```csharp
using Microsoft.AspNetCore.Authorization;

builder.Services.AddAuthorization();

builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
```

### Explicación

- `AddAuthorization()` habilita el sistema de autorización.
    
- `IAuthorizationPolicyProvider`:
    
    - Se registra como **Singleton** porque no depende del request.
        
- `IAuthorizationHandler`:
    
    - Se registra como **Scoped** por consistencia con el request.
        

---

## Parte 5: Agregar claims de permisos al JWT

### Extracción desde el usuario

```csharp
var permissions = user.PermisoUsuarios
    .Select(pu => pu.IdPermisoNavigation.Permiso1)
    .Where(p => !string.IsNullOrWhiteSpace(p))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToList();

var permissionClaims = permissions
    .Select(p => new Claim(AuthConstants.PermissionClaimType, p!));
```

### Inserción en el token

```csharp
claims.AddRange(permissionClaims);
```

---

## Parte 6: Uso en Controllers

Ejemplo:

```csharp
using Microsoft.AspNetCore.Authorization;

[Authorize(Policy = "PERM:USR_CREATE")]
[HttpPost("create")]
public async Task<IActionResult> CreateUser(...) { ... }
```

Otro ejemplo:

```csharp
[Authorize(Policy = "PERM:VEN_CREATE")]
[HttpPost("sales")]
public async Task<IActionResult> CreateSale(...) { ... }
```

---

## Cómo fluye todo (Resumen)

1. **Login** emite token con claims `permission`.
    
2. El frontend envía `Bearer <token>`.
    
3. JWT Bearer valida token → `HttpContext.User` queda poblado.
    
4. Un endpoint requiere `Policy = "PERM:XXX"`.
    
5. `PermissionPolicyProvider` genera la policy (requirement) al vuelo.
    
6. `PermissionHandler` valida que el claim exista.
    
7. Si existe → 200. Si no existe → 403.
    

---

## Códigos HTTP esperados

- **401 Unauthorized**: no hay token / token inválido / token vencido.
    
- **403 Forbidden**: token válido pero no tiene el permiso requerido.
    

---

## Debug rápido

Ver permisos del token en un endpoint:

```csharp
var perms = User.Claims
    .Where(c => c.Type == AuthConstants.PermissionClaimType)
    .Select(c => c.Value)
    .ToList();
```

---

## Errores típicos

1. **Siempre 403**
    
    - No se agregan claims `permission` al token
        
    - Policy mal escrita: `PERM:` + permiso incorrecto
        
2. **Siempre 401**
    
    - Falta `UseAuthentication()`
        
    - Firma/issuer/audience incorrectos
        
3. **Claims no cargados**
    
    - Login no incluye `PermisoUsuarios` + `Permiso` (falta Include)
        

---

## Conclusión

Este enfoque:

- Escala con muchos permisos
    
- Mantiene la API limpia
    
- Respeta el modelo de permisos existente
    
- Evita mantenimiento manual de policies

# Consultas - Autorización Dinámica en ASP.NET Core

Este parte del documento responde y explica:

- ¿Qué es `IAuthorizationRequirement`?
    
- ¿Hay que instalar alguna biblioteca?
    
- ¿Qué es `AuthorizationHandler<TRequirement>`?
    
- ¿Qué es `DefaultAuthorizationPolicyProvider`?
    
- Ejemplo paso a paso de qué ocurre al ejecutar:
    
    - `public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)`
        

> Contexto: se está implementando autorización por permisos usando policies dinámicas del tipo `PERM:<CODIGO>` y claims `permission=<CODIGO>`.

---

## 1) ¿Qué es `IAuthorizationRequirement`?

### Definición

`IAuthorizationRequirement` es una **interfaz marker** (sin métodos) que representa una **condición de autorización**.

En ASP.NET Core, una **Policy** está compuesta por uno o más _requirements_. Cada requirement define _qué se requiere_ para autorizar.

### Ejemplo

Si la regla es:

> “El usuario debe tener el permiso `USR_CREATE`”

Se modela con un requirement:

```csharp
public sealed class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }

    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }
}
```

### Concepto clave

- Requirement = “**qué condición** hay que cumplir”
    
- No define **cómo** se valida. Eso lo hace el _handler_.
    

---

## 2) ¿Tengo que instalar alguna biblioteca?

En la mayoría de proyectos ASP.NET Core Web API **NO**.

Estas clases pertenecen al framework:

- Namespace principal: `Microsoft.AspNetCore.Authorization`
    

Si tu proyecto ya referencia ASP.NET Core (Web API), normalmente ya está.

### Cuándo podría faltarte

Si estás en un proyecto muy mínimo o librería de clases aislada, podrías necesitar:

- `Microsoft.AspNetCore.Authorization`
    

Pero en Web API estándar suele venir incluido.

---

## 3) ¿Qué es `AuthorizationHandler<PermissionRequirement>`?

### Definición

`AuthorizationHandler<TRequirement>` es una clase base para implementar la lógica que **evalúa** un requirement.

- Requirement: “qué se necesita”
    
- Handler: “cómo comprobarlo”
    

### Ejemplo

```csharp
public sealed class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        // context.User contiene claims (del JWT validado)

        var hasPermission = context.User.Claims.Any(c =>
            c.Type == "permission" &&
            c.Value.Equals(requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasPermission)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
```

### Concepto técnico

- `AuthorizationHandlerContext` incluye:
    
    - `User` (ClaimsPrincipal)
        
    - el recurso (`Resource`) si aplica
        
    - el conjunto de requirements a evaluar
        
- `context.Succeed(requirement)` indica:
    
    - “este requirement fue cumplido”
        

Si ningún handler marca el requirement como exitoso:

- la autorización falla → **403 Forbidden** (si el usuario está autenticado)
    

---

## 4) ¿Qué es `DefaultAuthorizationPolicyProvider`?

### Definición

Un **Policy Provider** es el componente que **resuelve policies por nombre**.

Ejemplo:

```csharp
[Authorize(Policy = "PERM:USR_CREATE")]
```

ASP.NET Core necesita obtener el objeto `AuthorizationPolicy` asociado a ese string.

- El provider default (`DefaultAuthorizationPolicyProvider`) busca policies declaradas en `AddAuthorization(options => ...)`.
    
- Si no existe una policy con ese nombre, normalmente devuelve null.
    

### ¿Por qué extenderlo?

Porque queremos:

- No declarar 100 policies manualmente
    
- Construirlas dinámicamente cuando el nombre siga una convención (ej. `PERM:`)
    

---

## 5) ¿Qué hace `GetPolicyAsync(string policyName)` paso a paso?

### Código típico

```csharp
public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
{
    if (policyName.StartsWith("PERM:", StringComparison.OrdinalIgnoreCase))
    {
        var permission = policyName.Substring("PERM:".Length).Trim();

        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddRequirements(new PermissionRequirement(permission))
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(policy);
    }

    return base.GetPolicyAsync(policyName);
}
```

### Ejecución real: ejemplo paso a paso

Supongamos un endpoint:

```csharp
[Authorize(Policy = "PERM:USR_CREATE")]
public IActionResult CreateUser() => Ok();
```

Ahora llega un request.

#### Paso 1: autenticación (JWT)

- El middleware JWT Bearer valida token.
    
- Si es válido, crea `HttpContext.User` con claims.
    

#### Paso 2: se detecta `[Authorize]`

- El middleware de autorización ve que el endpoint requiere una policy llamada:
    
    - `policyName = "PERM:USR_CREATE"`
        

#### Paso 3: se llama al policy provider

- ASP.NET Core llama:
    

```csharp
GetPolicyAsync("PERM:USR_CREATE")
```

#### Paso 4: el provider intercepta

- Evalúa el prefijo:
    
    - ¿empieza con `PERM:`? → **Sí**
        

#### Paso 5: extrae el permiso

- Calcula:
    

```csharp
permission = "USR_CREATE"
```

#### Paso 6: construye la policy

Crea una `AuthorizationPolicy` con dos condiciones:

1. `RequireAuthenticatedUser()`
    
    - asegura que el usuario esté autenticado
        
2. `AddRequirements(new PermissionRequirement("USR_CREATE"))`
    
    - agrega el requirement específico
        

#### Paso 7: devuelve la policy

- Retorna esa policy al framework.
    

#### Paso 8: ejecución de handlers

- El framework toma la policy y ejecuta los handlers registrados.
    
- Tu `PermissionHandler` corre y revisa `User.Claims`:
    
    - busca `permission = USR_CREATE`
        

#### Paso 9: resultado

- Si existe claim → `context.Succeed(...)` → autorizado (200)
    
- Si NO existe claim → 403 Forbidden
    

---

## 6) Qué “produce” la policy dinámicamente

En términos prácticos, `PERM:USR_CREATE` se transforma en una policy equivalente a:

> “Usuario autenticado **y** debe tener el permiso USR_CREATE”.

Sin necesidad de escribir:

```csharp
options.AddPolicy("USR_CREATE", p => p.RequireClaim("permission", "USR_CREATE"));
```

---

## 7) Resumen final

- `IAuthorizationRequirement`: representa **una condición**.
    
- `AuthorizationHandler<T>`: implementa **cómo se valida** esa condición.
    
- `DefaultAuthorizationPolicyProvider`: resuelve policies por nombre; lo extendés para generar policies en runtime.
    
- `GetPolicyAsync`: toma el string `PERM:<X>` y construye una policy que exige `PermissionRequirement(X)`.
    

---

## Sugerencia de organización en tu proyecto

Ubicar la autorización compartida en:

```
Shared/
  Auth/
    AuthConstants.cs
    Authorization/
      PermissionRequirement.cs
      PermissionHandler.cs
      PermissionPolicyProvider.cs
```

De esta manera, se reutiliza en todas las features.