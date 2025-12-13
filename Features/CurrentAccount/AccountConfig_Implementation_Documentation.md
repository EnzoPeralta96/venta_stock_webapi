# AccountConfig - Documentación de Implementación Completa

> **Módulo**: Configuración de Cuenta Corriente (Current Account Configuration)
>
> **Fecha de Documentación**: 2025-12-12
>
> **Propósito**: Esta documentación detalla la implementación completa del módulo AccountConfig para ser utilizada como referencia en el desarrollo de la interfaz de usuario (UI) del módulo de configuración de cuenta corriente.

---

## 📋 Tabla de Contenidos

1. [Descripción General](#-descripción-general)
2. [Modelo de Datos y Esquema](#-modelo-de-datos-y-esquema)
3. [Data Transfer Objects (DTOs)](#-data-transfer-objects-dtos)
4. [Capa de Repositorio](#-capa-de-repositorio)
5. [Capa de Servicios](#-capa-de-servicios)
6. [Endpoints del Controlador](#-endpoints-del-controlador)
7. [AutoMapper Profiles](#-automapper-profiles)
8. [Manejo de Errores y Mensajes](#-manejo-de-errores-y-mensajes)
9. [Reglas de Negocio y Validaciones](#-reglas-de-negocio-y-validaciones)
10. [Integración con Otros Módulos](#-integración-con-otros-módulos)
11. [Configuración de Inyección de Dependencias](#-configuración-de-inyección-de-dependencias)
12. [Resumen de API para Frontend](#-resumen-de-api-para-frontend)
13. [Características Faltantes y Mejoras Potenciales](#-características-faltantes-y-mejoras-potenciales)
14. [Ubicación de Archivos](#-ubicación-de-archivos)

---

## 🎯 Descripción General

### Propósito del Negocio

**AccountConfig** (`ConfiguracionCc`) define configuraciones predefinidas de límites de crédito que pueden ser asignadas a clientes cuando habilitan Cuenta Corriente. Proporciona plantillas de límites de crédito (por ejemplo, "Bronce: $50,000", "Plata: $100,000", "Oro: $500,000").

### Características Principales

- ✅ CRUD completo (Crear, Leer, Actualizar, Toggle Estado)
- ✅ **Filtrado por estado** (activo/inactivo) en endpoint GET
- ✅ Validación de unicidad en nombre y monto límite
- ✅ Patrón de soft delete (campo `Activo`)
- ✅ Validaciones de negocio robustas
- ✅ Mensajes de error centralizados en español
- ✅ Uso del patrón Result para manejo de operaciones
- ✅ Ordenamiento automático por `MontoLimite` ascendente

### Arquitectura

El módulo sigue una arquitectura en capas estricta:

```
Controller (API Layer)
    ↓
Service (Business Logic Layer)
    ↓
Repository (Data Access Layer)
    ↓
Database (PostgreSQL via EF Core)
```

---

## 🗄️ Modelo de Datos y Esquema

### Entidad: ConfiguracionCc

**Archivo**: `Models/ConfiguracionCc.cs`

```csharp
public partial class ConfiguracionCc
{
    public int IdConfig { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal MontoLimite { get; set; }
    public bool Activo { get; set; } = true;
}
```

### Configuración de Base de Datos

**Archivo**: `Data/VentaStockContext.cs` (líneas 513-524)

**Tabla**: `configuracion_cc`

| Columna | Tipo | Restricciones | Descripción |
|---------|------|---------------|-------------|
| `id_config` | `int` | PRIMARY KEY, AUTO_INCREMENT | Identificador único |
| `nombre` | `varchar` | NOT NULL, UNIQUE INDEX | Nombre de la configuración |
| `monto_limite` | `decimal(18,2)` | NOT NULL, CHECK > 0 | Límite de crédito |
| `activo` | `boolean` | DEFAULT true | Estado activo/inactivo |

**Restricciones de Integridad**:
- ✅ Unique constraint en `Nombre`
- ✅ Check constraint: `monto_limite > 0`
- ⚠️ **NO tiene relaciones de clave foránea** (tabla independiente)

**Configuración Fluent API**:

```csharp
entity.ToTable("configuracion_cc");

entity.HasIndex(e => e.Nombre).IsUnique();

entity.Property(e => e.IdConfig).HasColumnName("id_config");
entity.Property(e => e.Activo)
    .HasDefaultValue(true)
    .HasColumnName("activo");
entity.Property(e => e.MontoLimite)
    .HasPrecision(18, 2)
    .HasColumnName("monto_limite");
entity.Property(e => e.Nombre).HasColumnName("nombre");
```

---

## 📦 Data Transfer Objects (DTOs)

### 1. AccountConfigDTO (Response/Display)

**Archivo**: `CurrentAccount/DTO/AccountConfigDTO/AccountConfigDTO.cs`

**Propósito**: DTO para respuestas de API y visualización en UI

```csharp
public class AccountConfigDTO
{
    public int IdConfig { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal MontoLimite { get; set; }
    public bool Activo { get; set; }
}
```

**Uso**:
- Respuesta de GET por ID
- Respuesta de GET lista completa
- Visualización en tablas y formularios

---

### 2. CreateAccountConfigDTO (Creación)

**Archivo**: `CurrentAccount/DTO/AccountConfigDTO/CreateAccountConfigDTO.cs`

**Propósito**: DTO para crear nuevas configuraciones

```csharp
public class CreateAccountConfigDTO
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El monto limite es obligatorio.")]
    public decimal MontoLimite { get; set; }
}
```

**Validaciones**:
- ✅ `Nombre`: Required
- ✅ `MontoLimite`: Required
- ✅ `Activo`: Se establece automáticamente en `true` via AutoMapper

**Ejemplo de Uso (Frontend)**:

```json
{
  "nombre": "Platinum",
  "montoLimite": 1000000
}
```

---

### 3. UpdateAccountConfigDTO (Actualización)

**Archivo**: `CurrentAccount/DTO/AccountConfigDTO/UpdateAccountConfigDTO.cs`

**Propósito**: DTO para actualizar configuraciones existentes

```csharp
public class UpdateAccountConfigDTO
{
    [Required(ErrorMessage = "El ID de la configuración es obligatorio.")]
    public int IdConfig { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El monto limite es obligatorio.")]
    public decimal MontoLimite { get; set; }
}
```

**Validaciones**:
- ✅ `IdConfig`: Required (identifica el registro a actualizar)
- ✅ `Nombre`: Required
- ✅ `MontoLimite`: Required
- ⚠️ `Activo`: **NO se puede actualizar** mediante este DTO (usar endpoint ToggleState)

**Ejemplo de Uso (Frontend)**:

```json
{
  "idConfig": 1,
  "nombre": "Bronze Premium",
  "montoLimite": 75000
}
```

---

## 🏗️ Capa de Repositorio

### Interface: IAccountConfigRepository

**Archivo**: `CurrentAccount/Repository/AccountConfigRepository/IAccountConfigRepository.cs`

**Métodos Definidos**:

```csharp
public interface IAccountConfigRepository
{
    // Consultas
    Task<ConfiguracionCc?> GetAccountConfigByIdAsync(int configId);
    Task<List<ConfiguracionCc>> GetAccountConfigsAsync();

    // Operaciones de Escritura
    Task CreateAccountConfigAsync(ConfiguracionCc config);
    Task<int> UpdateAccountConfigAsync(ConfiguracionCc config);
    Task ToggleStateAccountConfigAsync(int configId, bool active);

    // Validaciones de Existencia
    Task<bool> AccountConfigExistsByNameAsync(string name);
    Task<bool> AccountConfigExistsByNameAsync(int id, string name);
    Task<bool> AccountConfigExistsByLimitAsync(decimal limit);
    Task<bool> AccountConfigExistsByLimitAsync(int id, decimal limit);
}
```

---

### Implementación: AccountConfigRepository

**Archivo**: `CurrentAccount/Repository/AccountConfigRepository/AccountConfigRepository.cs`

**Dependencias**:
- `VentaStockContext` (DbContext de EF Core)

#### Métodos Implementados

##### 1. GetAccountConfigByIdAsync (líneas 16-18)

```csharp
public async Task<ConfiguracionCc?> GetAccountConfigByIdAsync(int configId)
{
    return await _context.ConfiguracionCcs.FirstOrDefaultAsync(c => c.IdConfig == configId);
}
```

**Características**:
- Retorna `null` si no se encuentra
- Usa `FirstOrDefaultAsync` para consulta simple
- Sin tracking de EF Core (puede añadir `.AsNoTracking()` para optimización)

---

##### 2. GetAccountConfigsAsync (líneas 20-30)

```csharp
public async Task<List<ConfiguracionCc>> GetAccountConfigsAsync(bool? activo = null)
{
    var query = _context.ConfiguracionCcs.AsQueryable();

    if (activo.HasValue)
    {
        query = query.Where(c => c.Activo == activo.Value);
    }

    return query.OrderBy(c => c.MontoLimite).ToListAsync();
}
```

**Características**:
- ✅ Soporta filtrado por estado activo/inactivo mediante parámetro `bool? activo`
- ✅ Si `activo = null`: retorna **TODAS** las configuraciones
- ✅ Si `activo = true`: retorna solo configuraciones **activas**
- ✅ Si `activo = false`: retorna solo configuraciones **inactivas**
- ✅ Ordenamiento por `MontoLimite` ascendente (Bronze → Platinum)

**Uso**:
```csharp
// Obtener todas
var todas = await repo.GetAccountConfigsAsync();

// Obtener solo activas
var activas = await repo.GetAccountConfigsAsync(true);

// Obtener solo inactivas
var inactivas = await repo.GetAccountConfigsAsync(false);
```

---

##### 3. CreateAccountConfigAsync (líneas 25-28)

```csharp
public async Task CreateAccountConfigAsync(ConfiguracionCc config)
{
    await _context.ConfiguracionCcs.AddAsync(config);
    await _context.SaveChangesAsync();
}
```

**Características**:
- `IdConfig` es generado automáticamente por la base de datos
- `Activo` se establece en `true` por defecto (via AutoMapper)
- SaveChangesAsync persiste el cambio

---

##### 4. UpdateAccountConfigAsync (líneas 31-39)

```csharp
public async Task<int> UpdateAccountConfigAsync(ConfiguracionCc config)
{
    int result = await _context.ConfiguracionCcs
        .Where(c => c.IdConfig == config.IdConfig)
        .ExecuteUpdateAsync(c => c
            .SetProperty(p => p.Nombre, config.Nombre)
            .SetProperty(p => p.MontoLimite, config.MontoLimite));

    return result;
}
```

**Características**:
- ✅ Usa `ExecuteUpdateAsync` (EF Core 7+ feature)
- ✅ **NO carga la entidad en memoria** (más eficiente)
- ✅ Actualiza solo campos especificados (`Nombre`, `MontoLimite`)
- ⚠️ **NO actualiza** el campo `Activo`
- Retorna número de filas afectadas (int)

**Ventaja**: Operación bulk más eficiente que cargar, modificar y guardar

---

##### 5. ToggleStateAccountConfigAsync (líneas 42-48)

```csharp
public async Task ToggleStateAccountConfigAsync(int configId, bool active)
{
    await _context.ConfiguracionCcs
        .Where(c => c.IdConfig == configId)
        .ExecuteUpdateAsync(c => c
            .SetProperty(p => p.Activo, active));
}
```

**Características**:
- Usa `ExecuteUpdateAsync` para eficiencia
- Solo actualiza el campo `Activo`
- Sin retorno (void Task)
- Parámetro `active`: `true` = activar, `false` = desactivar

---

##### 6. Métodos de Validación de Existencia

**AccountConfigExistsByNameAsync (string name)** - Para CREATE (líneas 51-56)

```csharp
public async Task<bool> AccountConfigExistsByNameAsync(string name)
{
    return await _context.ConfiguracionCcs
        .AnyAsync(c => c.Nombre == name);
}
```

**Uso**: Validar que el nombre no exista antes de crear

---

**AccountConfigExistsByNameAsync (int id, string name)** - Para UPDATE (líneas 59-64)

```csharp
public async Task<bool> AccountConfigExistsByNameAsync(int id, string name)
{
    return await _context.ConfiguracionCcs
        .AnyAsync(c => c.IdConfig != id && c.Nombre == name);
}
```

**Uso**: Validar que el nombre no exista en otros registros al actualizar

---

**AccountConfigExistsByLimitAsync (decimal limit)** - Para CREATE (líneas 66-72)

```csharp
public async Task<bool> AccountConfigExistsByLimitAsync(decimal limit)
{
    return await _context.ConfiguracionCcs
        .AnyAsync(c => c.MontoLimite == limit);
}
```

**Uso**: Validar que el monto límite no exista antes de crear

---

**AccountConfigExistsByLimitAsync (int id, decimal limit)** - Para UPDATE (líneas 74-80)

```csharp
public async Task<bool> AccountConfigExistsByLimitAsync(int id, decimal limit)
{
    return await _context.ConfiguracionCcs
        .AnyAsync(c => c.IdConfig != id && c.MontoLimite == limit);
}
```

**Uso**: Validar que el monto límite no exista en otros registros al actualizar

---

## ⚙️ Capa de Servicios

### Interface: IAccountConfigService

**Archivo**: `CurrentAccount/Services/AccountConfigService/IAccountConfigService.cs`

```csharp
public interface IAccountConfigService
{
    Task<Result<AccountConfigDTO>> GetAccountConfigById(int configId);
    Task<Result<List<AccountConfigDTO>>> GetAccountConfigs(bool? activo = null);
    Task<Result<string>> CreateAccountConfig(CreateAccountConfigDTO accountConfigDTO);
    Task<Result<string>> UpdateAccountConfig(UpdateAccountConfigDTO accountConfigDTO);
    Task<Result<string>> ToggleStateAccountConfig(int configId, bool active);
}
```

**Nota**: Todos los métodos retornan `Result<T>` para manejo consistente de errores

---

### Implementación: AccountConfigService

**Archivo**: `CurrentAccount/Services/AccountConfigService/AccountConfigService.cs`

**Dependencias Inyectadas**:
- `IAccountConfigRepository` - Acceso a datos
- `IMapper` - AutoMapper para conversiones DTO
- `ILogger<AccountConfigService>` - Logging

---

#### Métodos Implementados

##### 1. GetAccountConfigById (líneas 23-42)

**Propósito**: Obtener una configuración por ID

```csharp
public async Task<Result<AccountConfigDTO>> GetAccountConfigById(int configId)
{
    try
    {
        var result = await _accountConfigRepository.GetAccountConfigByIdAsync(configId);

        if (result == null)
        {
            return Result<AccountConfigDTO>.Failure(AccountConfigCode.account_config_not_found);
        }

        var accountConfigDTO = _mapper.Map<AccountConfigDTO>(result);

        return Result<AccountConfigDTO>.Succes(accountConfigDTO);
    }
    catch (Exception ex)
    {
        _logger.LogError("Error inesperado: " + ex);
        return Result<AccountConfigDTO>.Failure(AccountConfigCode.unexpected_error);
    }
}
```

**Flujo de Ejecución**:
1. ✅ Consulta repositorio por ID
2. ✅ Valida si existe (null check)
3. ✅ Mapea entidad a DTO
4. ✅ Retorna Result con AccountConfigDTO
5. ⚠️ Captura excepciones y registra en log

**Códigos de Error**:
- `account_config_not_found`: Configuración no existe
- `unexpected_error`: Error inesperado

---

##### 2. GetAccountConfigs (líneas 45-60)

**Propósito**: Obtener configuraciones con filtrado opcional por estado

```csharp
public async Task<Result<List<AccountConfigDTO>>> GetAccountConfigs(bool? activo = null)
{
    try
    {
        var configs = await _accountConfigRepository.GetAccountConfigsAsync(activo);

        var configsDTO = _mapper.Map<List<AccountConfigDTO>>(configs);

        return Result<List<AccountConfigDTO>>.Success(configsDTO);
    }
    catch (System.Exception ex)
    {
        _logger.LogError(ex, "Error retrieving account configs");
        return Result<List<AccountConfigDTO>>.Failure(AccountConfigCode.unexpected_error);
    }
}
```

**Flujo de Ejecución**:
1. ✅ Recibe parámetro opcional `activo` para filtrado
2. ✅ Obtiene configuraciones del repositorio (con filtro aplicado)
3. ✅ Mapea lista de entidades a lista de DTOs
4. ✅ Retorna Result con lista

**Parámetros**:
- `activo` (bool? nullable):
  - `null`: Retorna **todas** las configuraciones
  - `true`: Retorna solo configuraciones **activas**
  - `false`: Retorna solo configuraciones **inactivas**

**Características**:
- ✅ **Filtrado por estado** activo/inactivo
- ✅ Ordenamiento por `MontoLimite` ascendente (implementado en Repository)
- ⚠️ Sin paginación

**Códigos de Error**:
- `unexpected_error`: Error inesperado

**Uso**:
```csharp
// Obtener todas
var todas = await service.GetAccountConfigs();

// Obtener solo activas (para dropdown en módulo Cliente)
var activas = await service.GetAccountConfigs(true);

// Obtener solo inactivas
var inactivas = await service.GetAccountConfigs(false);
```

---

##### 3. CreateAccountConfig (líneas 63-87)

**Propósito**: Crear nueva configuración

```csharp
public async Task<Result<string>> CreateAccountConfig(CreateAccountConfigDTO accountConfigDTO)
{
    try
    {
        // Validación 1: Nombre único
        if (await _accountConfigRepository.AccountConfigExistsByNameAsync(accountConfigDTO.Nombre))
        {
            return Result<string>.Failure(AccountConfigCode.account_config_name_exists);
        }

        // Validación 2: Límite único
        if (await _accountConfigRepository.AccountConfigExistsByLimitAsync(accountConfigDTO.MontoLimite))
        {
            return Result<string>.Failure(AccountConfigCode.account_config_limit_exists);
        }

        var accountConfig = _mapper.Map<ConfiguracionCc>(accountConfigDTO);

        await _accountConfigRepository.CreateAccountConfigAsync(accountConfig);

        return Result<string>.Succes();
    }
    catch (Exception ex)
    {
        _logger.LogError("Error inesperado al crear la configuración: " + ex);
        return Result<string>.Failure(AccountConfigCode.unexpected_error);
    }
}
```

**Flujo de Ejecución**:
1. ✅ **Validación de Negocio 1**: Verifica que el nombre no exista
2. ✅ **Validación de Negocio 2**: Verifica que el monto límite no exista
3. ✅ Mapea DTO a entidad (con `Activo = true` automático)
4. ✅ Persiste en base de datos
5. ✅ Retorna resultado exitoso

**Códigos de Error**:
- `account_config_name_exists`: El nombre ya está registrado
- `account_config_limit_exists`: El límite ya está registrado
- `unexpected_error`: Error inesperado

**Regla de Negocio Importante**:
- ⚠️ **Tanto el nombre COMO el monto límite deben ser únicos**

---

##### 4. UpdateAccountConfig (líneas 90-114)

**Propósito**: Actualizar configuración existente

```csharp
public async Task<Result<string>> UpdateAccountConfig(UpdateAccountConfigDTO accountConfigDTO)
{
    try
    {
        // Validación 1: Nombre único (excluyendo el registro actual)
        if (await _accountConfigRepository.AccountConfigExistsByNameAsync(
            accountConfigDTO.IdConfig, accountConfigDTO.Nombre))
        {
            return Result<string>.Failure(AccountConfigCode.account_config_name_exists);
        }

        // Validación 2: Límite único (excluyendo el registro actual)
        if (await _accountConfigRepository.AccountConfigExistsByLimitAsync(
            accountConfigDTO.IdConfig, accountConfigDTO.MontoLimite))
        {
            return Result<string>.Failure(AccountConfigCode.account_config_limit_exists);
        }

        var accountConfig = _mapper.Map<ConfiguracionCc>(accountConfigDTO);

        await _accountConfigRepository.UpdateAccountConfigAsync(accountConfig);

        return Result<string>.Succes();
    }
    catch (Exception ex)
    {
        _logger.LogError("Error inesperado al creating la configuración: " + ex);
        return Result<string>.Failure(AccountConfigCode.unexpected_error);
    }
}
```

**Flujo de Ejecución**:
1. ✅ **Validación 1**: Nombre único (excluye el ID actual)
2. ✅ **Validación 2**: Monto límite único (excluye el ID actual)
3. ✅ Mapea DTO a entidad
4. ✅ Actualiza usando `ExecuteUpdateAsync`
5. ✅ Retorna resultado exitoso

**⚠️ Bug Encontrado (línea 112)**:
```csharp
_logger.LogError("Error inesperado al creating la configuración: " + ex);
```
**Debería decir**: "...al **actualizar** la configuración..."

**Códigos de Error**:
- `account_config_name_exists`: El nombre ya está en uso (en otro registro)
- `account_config_limit_exists`: El límite ya está en uso (en otro registro)
- `unexpected_error`: Error inesperado

---

##### 5. ToggleStateAccountConfig (líneas 117-135)

**Propósito**: Activar/Desactivar configuración (soft delete)

```csharp
public async Task<Result<string>> ToggleStateAccountConfig(int configId, bool active)
{
    try
    {
        // Validación: Verifica que exista la configuración
        var config = await _accountConfigRepository.GetAccountConfigByIdAsync(configId);
        if (config == null)
        {
            return Result<string>.Failure(AccountConfigCode.account_config_not_found);
        }

        await _accountConfigRepository.ToggleStateAccountConfigAsync(configId, active);

        return Result<string>.Succes();
    }
    catch (Exception ex)
    {
        _logger.LogError("Error inesperado: " + ex);
        return Result<string>.Failure(AccountConfigCode.unexpected_error);
    }
}
```

**Flujo de Ejecución**:
1. ✅ **Validación**: Verifica que la configuración exista
2. ✅ Actualiza solo el campo `Activo`
3. ✅ Retorna resultado exitoso

**Parámetros**:
- `configId`: ID de la configuración
- `active`: `true` = activar, `false` = desactivar

**Códigos de Error**:
- `account_config_not_found`: Configuración no existe
- `unexpected_error`: Error inesperado

**Notas**:
- ⚠️ No valida si la configuración está en uso por clientes
- ⚠️ No previene desactivar configuraciones activas en uso

---

## 🌐 Endpoints del Controlador

### Controller: CurrentAccountController

**Archivo**: `CurrentAccount/Controllers/CurrentAccountController.cs`

**Ruta Base**: `api/AccountConfig`

**Dependencia Inyectada**: `IAccountConfigService`

---

### Endpoints Disponibles

#### 1. GET - Obtener Configuración por ID

**Endpoint**: `GET /api/AccountConfig/account-configs/{configId}`

**Código** (líneas 20-33):

```csharp
[HttpGet("account-configs/{configId}")]
public async Task<IActionResult> GetAccountConfigById(int configId)
{
    var result = await _accountConfigService.GetAccountConfigById(configId);

    if (!result.IsSucces)
    {
        var code = (AccountConfigCode)result.ErrorCode;
        var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
        return NotFound(errorMessage);
    }

    return Ok(result.Value);
}
```

**Request**:
```
GET /api/AccountConfig/account-configs/1
```

**Response 200 OK**:
```json
{
  "idConfig": 1,
  "nombre": "Bronze",
  "montoLimite": 50000.00,
  "activo": true
}
```

**Response 404 Not Found**:
```json
"La configuración de cuenta indicada no existe."
```

**Características**:
- ✅ Retorna `AccountConfigDTO`
- ⚠️ Sin autenticación/autorización

---

#### 2. GET - Obtener Configuraciones (con filtrado opcional)

**Endpoint**: `GET /api/AccountConfig/account-configs?activo={true|false}`

**Código** (líneas 35-48):

```csharp
[HttpGet("account-configs")]
public async Task<IActionResult> GetAccountConfigs([FromQuery] bool? activo = null)
{
    var result = await _accountConfigService.GetAccountConfigs(activo);

    if (!result.IsSucces)
    {
        var code = (AccountConfigCode)result.ErrorCode;
        var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
        return NotFound(errorMessage);
    }

    return Ok(result.Value);
}
```

**Request Examples**:
```
GET /api/AccountConfig/account-configs              (Todas las configuraciones)
GET /api/AccountConfig/account-configs?activo=true  (Solo activas)
GET /api/AccountConfig/account-configs?activo=false (Solo inactivas)
```

**Response 200 OK** (todas):
```json
[
  {
    "idConfig": 1,
    "nombre": "Bronze",
    "montoLimite": 50000.00,
    "activo": true
  },
  {
    "idConfig": 2,
    "nombre": "Silver",
    "montoLimite": 100000.00,
    "activo": true
  },
  {
    "idConfig": 3,
    "nombre": "Gold",
    "montoLimite": 500000.00,
    "activo": false
  }
]
```

**Response 200 OK** (solo activas con `?activo=true`):
```json
[
  {
    "idConfig": 1,
    "nombre": "Bronze",
    "montoLimite": 50000.00,
    "activo": true
  },
  {
    "idConfig": 2,
    "nombre": "Silver",
    "montoLimite": 100000.00,
    "activo": true
  }
]
```

**Parámetros Query**:
- `activo` (bool? opcional):
  - No especificado o `null`: Retorna todas
  - `true`: Retorna solo configuraciones activas
  - `false`: Retorna solo configuraciones inactivas

**Características**:
- ✅ Retorna `List<AccountConfigDTO>`
- ✅ **Soporta filtrado por estado** mediante query parameter
- ✅ Ordenamiento por `MontoLimite` ascendente
- ⚠️ Sin paginación (retorna TODOS los registros del filtro)
- ⚠️ Sin autenticación/autorización

**Uso en Frontend**:
- **Sin parámetro**: Para admin panel mostrando todas las configuraciones
- **`?activo=true`**: Para dropdown en creación de clientes (solo activas)
- **`?activo=false`**: Para ver configuraciones desactivadas (historial)

---

#### 3. POST - Crear Nueva Configuración

**Endpoint**: `POST /api/AccountConfig/create-account-configs`

**Código** (líneas 50-68):

```csharp
[HttpPost("create-account-configs")]
public async Task<IActionResult> CreateAccountConfig([FromBody] CreateAccountConfigDTO accountConfigDTO)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    var result = await _accountConfigService.CreateAccountConfig(accountConfigDTO);

    if (!result.IsSucces)
    {
        var code = (AccountConfigCode)result.ErrorCode;
        var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
        return BadRequest(errorMessage);
    }

    return Ok();
}
```

**Request Body**:
```json
{
  "nombre": "Platinum",
  "montoLimite": 1000000
}
```

**Response 200 OK**:
```
(Empty response - successful creation)
```

**Response 400 Bad Request** (Validation Error):
```json
{
  "Nombre": ["El nombre es obligatorio."],
  "MontoLimite": ["El monto limite es obligatorio."]
}
```

**Response 400 Bad Request** (Business Rule Violation):
```json
"El nombre de la configuración de cuenta ya existe."
```
o
```json
"El límite de la configuración de cuenta ya existe."
```

**Características**:
- ✅ Validación de ModelState
- ✅ Validaciones de unicidad
- ⚠️ Sin autenticación/autorización

---

#### 4. PUT - Actualizar Configuración

**Endpoint**: `PUT /api/AccountConfig/update-account-configs`

**Código** (líneas 70-88):

```csharp
[HttpPut("update-account-configs")]
public async Task<IActionResult> UpdateAccountConfig([FromBody] UpdateAccountConfigDTO accountConfigDTO)
{
    if (!ModelState.IsValid)
    {
        return BadRequest(ModelState);
    }

    var result = await _accountConfigService.UpdateAccountConfig(accountConfigDTO);

    if (!result.IsSucces)
    {
        var code = (AccountConfigCode)result.ErrorCode;
        var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
        return BadRequest(errorMessage);
    }

    return Ok();
}
```

**Request Body**:
```json
{
  "idConfig": 1,
  "nombre": "Bronze Premium",
  "montoLimite": 75000
}
```

**Response 200 OK**:
```
(Empty response - successful update)
```

**Response 400 Bad Request** (Validation Error):
```json
{
  "IdConfig": ["El ID de la configuración es obligatorio."],
  "Nombre": ["El nombre es obligatorio."],
  "MontoLimite": ["El monto limite es obligatorio."]
}
```

**Response 400 Bad Request** (Business Rule Violation):
```json
"El nombre de la configuración de cuenta ya existe."
```

**Características**:
- ✅ Validación de ModelState
- ✅ Validaciones de unicidad (excluyendo el registro actual)
- ⚠️ **NO actualiza** el campo `Activo` (usar ToggleState)
- ⚠️ Sin autenticación/autorización

---

#### 5. DELETE - Toggle Estado (Activar/Desactivar)

**Endpoint**: `DELETE /api/AccountConfig/toggle-state/{configId}/{active}`

**Código** (líneas 90-103):

```csharp
[HttpDelete("toggle-state/{configId}/{active}")]
public async Task<IActionResult> ToggleStateAccountConfig(int configId, bool active)
{
    var result = await _accountConfigService.ToggleStateAccountConfig(configId, active);

    if (!result.IsSucces)
    {
        var code = (AccountConfigCode)result.ErrorCode;
        var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
        return BadRequest(errorMessage);
    }

    return Ok();
}
```

**Request Examples**:
```
DELETE /api/AccountConfig/toggle-state/1/false   (Desactivar configuración ID 1)
DELETE /api/AccountConfig/toggle-state/1/true    (Activar configuración ID 1)
```

**Response 200 OK**:
```
(Empty response - successful toggle)
```

**Response 400 Bad Request**:
```json
"La configuración de cuenta indicada no existe."
```

**Características**:
- ⚠️ **Usa verbo DELETE** pero NO elimina físicamente (soft delete)
- ✅ Permite activar/desactivar mediante parámetro booleano
- ⚠️ Sin validación de uso (no verifica si está asignada a clientes)
- ⚠️ Sin autenticación/autorización

---

## 🔄 AutoMapper Profiles

### Profile: CurrentAccountConfigProfile

**Archivo**: `CurrentAccount/Profile/CurrentAccountConfigProfile.cs`

**Código** (líneas 6-19):

```csharp
public class CurrentAccountConfigProfile : Profile
{
    public CurrentAccountConfigProfile()
    {
        // CreateDTO → Entity (Activo = true por defecto)
        CreateMap<CreateAccountConfigDTO, ConfiguracionCc>()
            .ForMember(dest => dest.IdConfig, opt => opt.Ignore())
            .ForMember(dest => dest.Activo, opt => opt.MapFrom(src => true));

        // UpdateDTO → Entity (Activo ignorado)
        CreateMap<UpdateAccountConfigDTO, ConfiguracionCc>()
            .ForMember(dest => dest.Activo, opt => opt.Ignore());

        // Entity → ResponseDTO
        CreateMap<ConfiguracionCc, AccountConfigDTO>();
    }
}
```

### Mapeos Configurados

#### 1. CreateAccountConfigDTO → ConfiguracionCc

**Propósito**: Mapeo para creación

```csharp
CreateMap<CreateAccountConfigDTO, ConfiguracionCc>()
    .ForMember(dest => dest.IdConfig, opt => opt.Ignore())      // DB auto-generates
    .ForMember(dest => dest.Activo, opt => opt.MapFrom(src => true));  // Default = true
```

**Comportamiento**:
- ✅ `IdConfig`: **Ignorado** (generado por base de datos)
- ✅ `Nombre`: Mapeado automáticamente
- ✅ `MontoLimite`: Mapeado automáticamente
- ✅ `Activo`: Establecido en `true` por defecto

---

#### 2. UpdateAccountConfigDTO → ConfiguracionCc

**Propósito**: Mapeo para actualización

```csharp
CreateMap<UpdateAccountConfigDTO, ConfiguracionCc>()
    .ForMember(dest => dest.Activo, opt => opt.Ignore());  // No se actualiza vía Update
```

**Comportamiento**:
- ✅ `IdConfig`: Mapeado (identifica el registro)
- ✅ `Nombre`: Mapeado
- ✅ `MontoLimite`: Mapeado
- ⚠️ `Activo`: **Ignorado** (debe usar ToggleState endpoint)

---

#### 3. ConfiguracionCc → AccountConfigDTO

**Propósito**: Mapeo para respuestas

```csharp
CreateMap<ConfiguracionCc, AccountConfigDTO>();
```

**Comportamiento**:
- ✅ Mapeo directo de todas las propiedades (convención por nombre)
- ✅ Usado en respuestas GET

---

## ⚠️ Manejo de Errores y Mensajes

### Error Codes Enum

**Archivo**: `CurrentAccount/Message/AccountConfigErrorCode.cs`

**Enumeración** (líneas 3-13):

```csharp
public enum AccountConfigCode
{
    account_config_not_found,           // ✅ USADO
    account_config_already_active,      // ❌ NO USADO
    account_config_name_exists,         // ✅ USADO
    account_config_limit_exists,        // ✅ USADO
    account_config_already_inactive,    // ❌ NO USADO
    account_config_creation_failed,     // ❌ NO USADO
    account_config_update_failed,       // ❌ NO USADO
    unexpected_error                    // ✅ USADO
}
```

---

### Error Messages Dictionary

**Diccionario** (líneas 15-28):

```csharp
public static class AccountConfigDictionary
{
    public static readonly Dictionary<AccountConfigCode, string> Messages = new()
    {
        { AccountConfigCode.account_config_not_found,
          "La configuración de cuenta indicada no existe." },

        { AccountConfigCode.account_config_already_active,
          "La configuración de cuenta ya está activa." },

        { AccountConfigCode.account_config_name_exists,
          "El nombre de la configuración de cuenta ya existe." },

        { AccountConfigCode.account_config_limit_exists,
          "El límite de la configuración de cuenta ya existe." },

        { AccountConfigCode.account_config_already_inactive,
          "La configuración de cuenta ya está inactiva." },

        { AccountConfigCode.account_config_creation_failed,
          "La creación de la configuración de cuenta falló." },

        { AccountConfigCode.account_config_update_failed,
          "La actualización de la configuración de cuenta falló." },

        { AccountConfigCode.unexpected_error,
          "Ocurrió un error inesperado, por favor intente nuevamente." }
    };
}
```

---

### Uso en Controllers

**Patrón de Uso**:

```csharp
if (!result.IsSucces)
{
    var code = (AccountConfigCode)result.ErrorCode;
    var errorMessage = MessageProvider.Get(AccountConfigDictionary.Messages, code);
    return BadRequest(errorMessage);  // o NotFound según el contexto
}
```

**Beneficios del Patrón**:
- ✅ Mensajes centralizados en español
- ✅ Fácil de mantener y actualizar
- ✅ Type-safe con enum
- ✅ Respuestas consistentes en toda la API

---

### Códigos de Error Actualmente Utilizados

| Código | Mensaje | Usado en |
|--------|---------|----------|
| `account_config_not_found` | "La configuración de cuenta indicada no existe." | GetById, ToggleState |
| `account_config_name_exists` | "El nombre de la configuración de cuenta ya existe." | Create, Update |
| `account_config_limit_exists` | "El límite de la configuración de cuenta ya existe." | Create, Update |
| `unexpected_error` | "Ocurrió un error inesperado, por favor intente nuevamente." | Todos los catch blocks |

---

### Códigos de Error NO Utilizados

| Código | Mensaje | Motivo |
|--------|---------|--------|
| `account_config_already_active` | "La configuración de cuenta ya está activa." | ToggleState no valida estado previo |
| `account_config_already_inactive` | "La configuración de cuenta ya está inactiva." | ToggleState no valida estado previo |
| `account_config_creation_failed` | "La creación de la configuración de cuenta falló." | Se usa `unexpected_error` genérico |
| `account_config_update_failed` | "La actualización de la configuración de cuenta falló." | Se usa `unexpected_error` genérico |

**Sugerencia**: Estos códigos podrían eliminarse o implementarse para mensajes más específicos.

---

## 📏 Reglas de Negocio y Validaciones

### Validaciones de Datos

#### 1. Validaciones de Campo (DTOs)

**CreateAccountConfigDTO**:
- ✅ `Nombre`: Required (obligatorio)
- ✅ `MontoLimite`: Required (obligatorio)

**UpdateAccountConfigDTO**:
- ✅ `IdConfig`: Required (obligatorio)
- ✅ `Nombre`: Required (obligatorio)
- ✅ `MontoLimite`: Required (obligatorio)

---

#### 2. Validaciones de Negocio (Service Layer)

**Restricción de Unicidad - Nombre**:
- ✅ El `Nombre` debe ser **único** en toda la tabla
- ✅ Al crear: valida que no exista ningún registro con ese nombre
- ✅ Al actualizar: valida que no exista en otros registros (excluye el ID actual)

**Restricción de Unicidad - Monto Límite**:
- ✅ El `MontoLimite` debe ser **único** en toda la tabla
- ✅ Al crear: valida que no exista ningún registro con ese límite
- ✅ Al actualizar: valida que no exista en otros registros (excluye el ID actual)

**⚠️ Regla Importante**:
```
Tanto el NOMBRE como el MONTO LÍMITE deben ser únicos.
Dos configuraciones NO pueden tener el mismo límite,
incluso si tienen nombres diferentes.
```

**Ejemplo Válido**:
```json
[
  { "nombre": "Bronze", "montoLimite": 50000 },
  { "nombre": "Silver", "montoLimite": 100000 },
  { "nombre": "Gold", "montoLimite": 500000 }
]
```

**Ejemplo Inválido**:
```json
[
  { "nombre": "Bronze Standard", "montoLimite": 50000 },
  { "nombre": "Bronze Premium", "montoLimite": 50000 }  // ❌ Límite duplicado
]
```

---

#### 3. Validaciones de Base de Datos

**Check Constraint**:
```sql
CHECK (monto_limite > 0)
```
- ✅ `MontoLimite` debe ser mayor que 0
- ⚠️ Esta validación NO está implementada en DTOs (solo en DB)

**Unique Index**:
```sql
CREATE UNIQUE INDEX ON configuracion_cc(nombre);
```
- ✅ `Nombre` tiene índice único en base de datos
- ✅ Refuerza la unicidad a nivel de DB

---

### Comportamiento de Soft Delete

**Campo `Activo`**:
- ✅ `true`: Configuración activa (disponible para asignar a clientes)
- ✅ `false`: Configuración inactiva (soft delete)

**Operaciones**:
1. **Crear**: Siempre se crea con `Activo = true`
2. **Actualizar**: NO permite modificar `Activo` (usar ToggleState)
3. **ToggleState**: Cambia solo el campo `Activo`
4. **Eliminar físicamente**: NO implementado

**⚠️ Limitaciones Actuales**:
- No valida si una configuración inactiva está en uso por clientes
- No previene desactivar configuraciones asignadas activamente

---

### Valores por Defecto

| Campo | Valor por Defecto | Establecido por |
|-------|-------------------|-----------------|
| `IdConfig` | Auto-increment | Base de datos |
| `Activo` | `true` | AutoMapper (CreateDTO) |
| `Nombre` | - | Required (debe proporcionarse) |
| `MontoLimite` | - | Required (debe proporcionarse) |

---

## 🔗 Integración con Otros Módulos

### Integración con Módulo de Clientes

**Relación Conceptual**:

```
AccountConfig (configuracion_cc)
    ↓ (proporciona plantillas de límites)
Cliente.LimiteCuenta
    ↓ (usado en)
MovimientoCc.LimiteCuenta
```

**⚠️ Nota Importante**:
- **NO existe relación de clave foránea** entre `ConfiguracionCc` y `Cliente`
- La integración es **implícita** mediante valores `decimal` coincidentes
- El frontend debe mostrar las configuraciones disponibles y el usuario selecciona una

---

### Flujo de Negocio

**Escenario**: Crear un cliente con Cuenta Corriente

1. **Frontend consulta** configuraciones disponibles:
   ```
   GET /api/AccountConfig/account-configs
   ```

2. **Frontend muestra** dropdown/select con opciones:
   ```
   - Bronze: $50,000
   - Silver: $100,000
   - Gold: $500,000
   ```

3. **Usuario selecciona** una configuración (ej: "Silver")

4. **Frontend envía** al crear cliente:
   ```json
   {
     "nombre": "Juan Pérez",
     "dni": "12345678",
     "tieneCuentaCorriente": true,
     "limiteCuenta": 100000,   // ← Valor del MontoLimite seleccionado
     "saldoInicial": 0
   }
   ```

5. **Backend guarda** en tabla `Cliente`:
   - `limite_cuenta = 100000` (mismo valor que en ConfiguracionCc)

6. **Uso posterior**: Cuando se crean movimientos de cuenta corriente, se usa el `limite_cuenta` del cliente

---

### Ejemplo de DTO de Cliente (Referencia)

**ClientCreateDTO** (del módulo Client):

```csharp
public class ClientCreateDTO
{
    // ... otros campos ...

    public bool TieneCuentaCorriente { get; set; }
    public decimal? LimiteCuenta { get; set; }      // ← Valor de AccountConfig.MontoLimite
    public decimal? SaldoInicial { get; set; }
}
```

**Validación Sugerida en ClientService**:
```csharp
if (clientDTO.TieneCuentaCorriente)
{
    if (!clientDTO.LimiteCuenta.HasValue || clientDTO.LimiteCuenta <= 0)
    {
        return Result.Failure("Debe especificar un límite de cuenta válido.");
    }

    // OPCIONAL: Validar que el límite coincida con una configuración activa
    var configExists = await _accountConfigRepository
        .AccountConfigExistsByLimitAsync(clientDTO.LimiteCuenta.Value);

    if (!configExists)
    {
        return Result.Failure("El límite seleccionado no corresponde a ninguna configuración válida.");
    }
}
```

---

### Datos de Ejemplo

**Tabla: configuracion_cc**

| id_config | nombre | monto_limite | activo |
|-----------|--------|--------------|--------|
| 1 | Bronze | 50000.00 | true |
| 2 | Silver | 100000.00 | true |
| 3 | Gold | 500000.00 | true |
| 4 | Platinum | 1000000.00 | true |
| 5 | VIP | 5000000.00 | false |

**Tabla: cliente** (ejemplo de integración)

| id_cliente | nombre | dni | tiene_cc | limite_cuenta | saldo_actual |
|------------|--------|-----|----------|---------------|--------------|
| 1 | Juan Pérez | 12345678 | true | 100000.00 | 25000.00 |
| 2 | María González | 87654321 | true | 500000.00 | 150000.00 |
| 3 | Pedro Martínez | 11223344 | false | null | null |

**Observación**:
- Juan Pérez tiene límite $100,000 (coincide con "Silver")
- María González tiene límite $500,000 (coincide con "Gold")
- Pedro Martínez no tiene cuenta corriente

---

## ⚙️ Configuración de Inyección de Dependencias

### Registro en Program.cs

**Archivo**: `Program.cs` (líneas 48-54)

```csharp
// AccountConfig Repository
builder.Services.AddScoped<IAccountConfigRepository, AccountConfigRepository>();

// AccountConfig Service
builder.Services.AddScoped<IAccountConfigService, AccountConfigService>();

// AutoMapper (escanea todos los perfiles del assembly)
builder.Services.AddAutoMapper(typeof(Program));
```

**Características**:
- ✅ **Scoped Lifetime**: Una instancia por request HTTP
- ✅ Patrón de inyección de dependencias estándar
- ✅ AutoMapper configurado globalmente

**Dependencias Transitivas**:
```
AccountConfigController
    ↓ (inyecta)
IAccountConfigService → AccountConfigService
    ↓ (inyecta)
IAccountConfigRepository → AccountConfigRepository
    ↓ (inyecta)
VentaStockContext (DbContext)
```

---

## 📡 Resumen de API para Frontend

### Base URL

```
http://localhost:5173/api/AccountConfig
```

⚠️ **Nota**: Ajustar según configuración de `launchSettings.json`

---

### Endpoints Disponibles

#### 1️⃣ GET - Listar Configuraciones (con filtrado opcional)

```http
GET /api/AccountConfig/account-configs
GET /api/AccountConfig/account-configs?activo=true
GET /api/AccountConfig/account-configs?activo=false
```

**Parámetros Query**:
- `activo` (bool? opcional):
  - Sin especificar: Retorna todas las configuraciones
  - `true`: Solo configuraciones activas
  - `false`: Solo configuraciones inactivas

**Response 200 OK** (todas):
```json
[
  {
    "idConfig": 1,
    "nombre": "Bronze",
    "montoLimite": 50000.00,
    "activo": true
  },
  {
    "idConfig": 2,
    "nombre": "Silver",
    "montoLimite": 100000.00,
    "activo": true
  },
  {
    "idConfig": 3,
    "nombre": "Gold",
    "montoLimite": 500000.00,
    "activo": false
  }
]
```

**Response 200 OK** (solo activas con `?activo=true`):
```json
[
  {
    "idConfig": 1,
    "nombre": "Bronze",
    "montoLimite": 50000.00,
    "activo": true
  },
  {
    "idConfig": 2,
    "nombre": "Silver",
    "montoLimite": 100000.00,
    "activo": true
  }
]
```

**Uso en Frontend**:
- **Sin filtro**: Admin panel - tabla mostrando todas las configuraciones
- **`?activo=true`**: Dropdown en creación de clientes (solo mostrar activas)
- **`?activo=false`**: Vista de configuraciones desactivadas (historial)
- Ordenadas por `MontoLimite` ascendente (Bronze → Platinum)

---

#### 2️⃣ GET - Obtener Configuración por ID

```http
GET /api/AccountConfig/account-configs/{id}
```

**Ejemplo**:
```http
GET /api/AccountConfig/account-configs/1
```

**Response 200 OK**:
```json
{
  "idConfig": 1,
  "nombre": "Bronze",
  "montoLimite": 50000.00,
  "activo": true
}
```

**Response 404 Not Found**:
```json
"La configuración de cuenta indicada no existe."
```

**Uso en Frontend**:
- Cargar datos en formulario de edición
- Vista de detalle de configuración

---

#### 3️⃣ POST - Crear Nueva Configuración

```http
POST /api/AccountConfig/create-account-configs
Content-Type: application/json

{
  "nombre": "Platinum",
  "montoLimite": 1000000
}
```

**Response 200 OK**:
```
(Empty body)
```

**Response 400 Bad Request** (Validación):
```json
{
  "Nombre": ["El nombre es obligatorio."],
  "MontoLimite": ["El monto limite es obligatorio."]
}
```

**Response 400 Bad Request** (Business Rule):
```json
"El nombre de la configuración de cuenta ya existe."
```
o
```json
"El límite de la configuración de cuenta ya existe."
```

**Uso en Frontend**:
- Formulario de creación
- Validar campos requeridos antes de enviar
- Mostrar errores específicos al usuario

---

#### 4️⃣ PUT - Actualizar Configuración

```http
PUT /api/AccountConfig/update-account-configs
Content-Type: application/json

{
  "idConfig": 1,
  "nombre": "Bronze Premium",
  "montoLimite": 75000
}
```

**Response 200 OK**:
```
(Empty body)
```

**Response 400 Bad Request** (Validación):
```json
{
  "IdConfig": ["El ID de la configuración es obligatorio."],
  "Nombre": ["El nombre es obligatorio."],
  "MontoLimite": ["El monto limite es obligatorio."]
}
```

**Response 400 Bad Request** (Business Rule):
```json
"El nombre de la configuración de cuenta ya existe."
```

**Uso en Frontend**:
- Formulario de edición
- Pre-cargar datos con GET by ID
- ⚠️ **NO permite actualizar campo `Activo`** (usar ToggleState)

---

#### 5️⃣ DELETE - Toggle Estado (Activar/Desactivar)

```http
DELETE /api/AccountConfig/toggle-state/{configId}/{active}
```

**Ejemplos**:
```http
DELETE /api/AccountConfig/toggle-state/1/false   (Desactivar)
DELETE /api/AccountConfig/toggle-state/1/true    (Activar)
```

**Response 200 OK**:
```
(Empty body)
```

**Response 400 Bad Request**:
```json
"La configuración de cuenta indicada no existe."
```

**Uso en Frontend**:
- Toggle switch o botón "Activar/Desactivar"
- No confundir con eliminación física (es soft delete)
- Actualizar estado en tabla tras operación exitosa

---

### Manejo de Errores en Frontend

**Patrón Recomendado**:

```typescript
async function createAccountConfig(data: CreateAccountConfigDTO) {
  try {
    const response = await fetch('/api/AccountConfig/create-account-configs', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    });

    if (!response.ok) {
      const errorMessage = await response.text();

      // Puede ser un string con el mensaje de error del diccionario
      // o un objeto JSON con errores de validación
      try {
        const validationErrors = JSON.parse(errorMessage);
        // Mostrar errores de validación por campo
        showValidationErrors(validationErrors);
      } catch {
        // Es un mensaje de error de negocio
        showErrorToast(errorMessage);
      }

      return;
    }

    showSuccessToast('Configuración creada exitosamente');
    refreshConfigList();
  } catch (error) {
    showErrorToast('Error de conexión con el servidor');
  }
}
```

---

### Componentes de UI Sugeridos

#### Tabla de Listado

**Columnas**:
- ID
- Nombre
- Monto Límite (formateado como moneda)
- Estado (Badge: Activo/Inactivo)
- Acciones (Editar, Toggle Estado)

**Filtros**:
- Buscar por nombre
- Filtrar por estado (Todos/Activos/Inactivos)
- Ordenar por nombre o monto límite

---

#### Formulario de Creación/Edición

**Campos**:
- **Nombre**: Input text (required)
- **Monto Límite**: Input number (required, min=0.01, step=0.01)

**Validaciones Frontend**:
```typescript
interface FormValidation {
  nombre: {
    required: true,
    maxLength: 50  // Ajustar según necesidad
  },
  montoLimite: {
    required: true,
    min: 0.01,
    pattern: /^\d+(\.\d{1,2})?$/  // Formato decimal
  }
}
```

**Botones**:
- Guardar (submit)
- Cancelar (reset form)

---

#### Toggle Estado

**Componente Sugerido**: Switch toggle

```tsx
<Switch
  checked={config.activo}
  onChange={(checked) => handleToggleState(config.idConfig, checked)}
  label={config.activo ? 'Activo' : 'Inactivo'}
/>
```

**Confirmación Recomendada**:
```typescript
async function handleToggleState(configId: number, newState: boolean) {
  const action = newState ? 'activar' : 'desactivar';

  const confirmed = await showConfirmDialog(
    `¿Está seguro que desea ${action} esta configuración?`
  );

  if (!confirmed) return;

  // Llamar a API
  await toggleStateAccountConfig(configId, newState);
}
```

---

#### Selector para Módulo Cliente

**Cuando se crea/edita cliente con Cuenta Corriente**:

```tsx
{tieneCuentaCorriente && (
  <Select
    label="Configuración de Límite"
    options={accountConfigs
      .filter(c => c.activo)  // Solo mostrar activas
      .map(c => ({
        value: c.montoLimite,
        label: `${c.nombre} - ${formatCurrency(c.montoLimite)}`
      }))
    }
    onChange={(value) => setLimiteCuenta(value)}
    required
  />
)}
```

**Formato Sugerido**:
```
Bronze - $ 50,000.00
Silver - $ 100,000.00
Gold - $ 500,000.00
Platinum - $ 1,000,000.00
```

---

## 🚀 Características Faltantes y Mejoras Potenciales

### Funcionalidades No Implementadas

#### 1. Paginación

**Problema Actual**:
- `GetAccountConfigs` retorna **TODOS** los registros sin paginación

**Impacto**:
- Ineficiente con grandes volúmenes de datos
- Carga innecesaria en cliente y servidor

**Solución Sugerida**:

```csharp
// Service
public async Task<Result<PagedList<AccountConfigDTO>>> GetAccountConfigsPaged(
    int pageIndex = 1,
    int pageSize = 10,
    string searchTerm = "",
    bool? activo = null)
{
    var query = _accountConfigRepository.AccountConfigsQueryable();

    // Filtro por búsqueda
    if (!string.IsNullOrEmpty(searchTerm))
    {
        query = query.Where(c => c.Nombre.Contains(searchTerm));
    }

    // Filtro por estado
    if (activo.HasValue)
    {
        query = query.Where(c => c.Activo == activo.Value);
    }

    // Ordenamiento
    query = query.OrderBy(c => c.Nombre);

    // Proyección y paginación
    var projected = _mapper.ProjectTo<AccountConfigDTO>(query);
    var paged = await PagedList<AccountConfigDTO>.CreateAsync(projected, pageIndex, pageSize);

    return Result<PagedList<AccountConfigDTO>>.Succes(paged);
}
```

---

#### 2. Filtrado y Búsqueda Avanzada

**Filtros Implementados**:
- ✅ Por estado (Activo/Inactivo/Todos) - mediante query parameter `?activo=true|false`

**Filtros Adicionales Sugeridos**:
- 🔜 Por nombre (búsqueda parcial con `?search=nombre`)
- 🔜 Por rango de monto límite (min-max)

**Ordenamiento Implementado**:
- ✅ Por monto límite ascendente (default)

**Ordenamiento Adicional Sugerido**:
- 🔜 Por nombre (A-Z, Z-A)
- 🔜 Por monto límite (permitir descendente)
- 🔜 Query parameter `?orderBy=nombre&direction=asc`

---

#### 3. Autenticación y Autorización

**Problema Actual**:
- ⚠️ **NINGÚN endpoint tiene autenticación**
- ⚠️ Cualquiera puede crear/modificar/eliminar configuraciones

**Solución Sugerida**:

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // ← Requiere autenticación
public class CurrentAccountController : ControllerBase
{
    [HttpGet("account-configs")]
    [Authorize(Roles = "Administrador,Encargado")]  // ← Solo roles específicos
    public async Task<IActionResult> GetAccountConfigs() { ... }

    [HttpPost("create-account-configs")]
    [Authorize(Roles = "Administrador")]  // ← Solo administrador
    public async Task<IActionResult> CreateAccountConfig(...) { ... }

    [HttpPut("update-account-configs")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> UpdateAccountConfig(...) { ... }

    [HttpDelete("toggle-state/{configId}/{active}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> ToggleStateAccountConfig(...) { ... }
}
```

---

#### 4. Auditoría

**Campos Faltantes en Modelo**:

```csharp
public partial class ConfiguracionCc
{
    public int IdConfig { get; set; }
    public string Nombre { get; set; } = null!;
    public decimal MontoLimite { get; set; }
    public bool Activo { get; set; } = true;

    // CAMPOS FALTANTES (sugeridos):
    public DateTime FechaCreacion { get; set; }         // ← Timestamp de creación
    public int? IdUsuarioCreador { get; set; }          // ← Quién creó
    public DateTime? FechaModificacion { get; set; }    // ← Última modificación
    public int? IdUsuarioModificador { get; set; }      // ← Quién modificó
    public DateTime? FechaBaja { get; set; }            // ← Timestamp de desactivación
}
```

**Beneficios**:
- Trazabilidad completa de operaciones
- Auditoría de cambios
- Cumplimiento de requisitos de seguridad

---

#### 5. Validación de Uso en Cascade

**Problema Actual**:
- ToggleState NO verifica si la configuración está en uso por clientes

**Escenario Problemático**:
1. Configuración "Silver" ($100,000) está asignada a 50 clientes
2. Administrador desactiva "Silver"
3. Clientes siguen teniendo `limite_cuenta = 100000` pero la config no está disponible
4. Al editar cliente, el límite actual no aparece en opciones

**Solución Sugerida**:

```csharp
// Repository
public async Task<bool> IsAccountConfigInUseAsync(decimal montoLimite)
{
    return await _context.Clientes
        .AnyAsync(c => c.LimiteCuenta == montoLimite && c.FechaBaja == null);
}

// Service
public async Task<Result<string>> ToggleStateAccountConfig(int configId, bool active)
{
    var config = await _accountConfigRepository.GetAccountConfigByIdAsync(configId);
    if (config == null)
    {
        return Result<string>.Failure(AccountConfigCode.account_config_not_found);
    }

    // Validación adicional: si se está desactivando
    if (!active)
    {
        var inUse = await _accountConfigRepository.IsAccountConfigInUseAsync(config.MontoLimite);
        if (inUse)
        {
            return Result<string>.Failure(AccountConfigCode.account_config_in_use);
        }
    }

    await _accountConfigRepository.ToggleStateAccountConfigAsync(configId, active);
    return Result<string>.Succes();
}
```

**Nuevo error code**:
```csharp
account_config_in_use → "No se puede desactivar la configuración porque está asignada a clientes activos."
```

---

#### 6. Validación de Monto Límite en DTOs

**Problema Actual**:
- Check constraint `monto_limite > 0` solo en base de datos
- No hay validación en DTOs

**Solución Sugerida**:

```csharp
public class CreateAccountConfigDTO
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [MaxLength(100, ErrorMessage = "El nombre no puede exceder 100 caracteres.")]
    public string Nombre { get; set; } = null!;

    [Required(ErrorMessage = "El monto limite es obligatorio.")]
    [Range(0.01, double.MaxValue, ErrorMessage = "El monto límite debe ser mayor a 0.")]
    public decimal MontoLimite { get; set; }
}
```

---

### Mejoras de Performance

#### 1. Agregar `.AsNoTracking()` en Consultas de Solo Lectura

**Archivo**: `AccountConfigRepository.cs`

```csharp
public async Task<ConfiguracionCc?> GetAccountConfigByIdAsync(int configId)
{
    return await _context.ConfiguracionCcs
        .AsNoTracking()  // ← Mejora performance
        .FirstOrDefaultAsync(c => c.IdConfig == configId);
}

public async Task<List<ConfiguracionCc>> GetAccountConfigsAsync()
{
    return await _context.ConfiguracionCcs
        .AsNoTracking()  // ← Mejora performance
        .OrderBy(c => c.Nombre)
        .ToListAsync();
}
```

**Beneficio**: Reduce overhead de EF Core al no trackear entidades

---

#### 2. Implementar Caching

**Para GetAccountConfigs** (cambia raramente):

```csharp
public async Task<Result<List<AccountConfigDTO>>> GetAccountConfigs()
{
    const string cacheKey = "account_configs_all";

    if (_cache.TryGetValue(cacheKey, out List<AccountConfigDTO> cached))
    {
        return Result<List<AccountConfigDTO>>.Succes(cached);
    }

    var result = await _accountConfigRepository.GetAccountConfigsAsync();
    var accountConfigDTO = _mapper.Map<List<AccountConfigDTO>>(result);

    _cache.Set(cacheKey, accountConfigDTO, TimeSpan.FromMinutes(30));

    return Result<List<AccountConfigDTO>>.Succes(accountConfigDTO);
}
```

**Invalidar cache** al crear/actualizar/toggle:
```csharp
_cache.Remove("account_configs_all");
```

---

### Mejoras de UX

#### 1. Ordenamiento por Defecto

**Sugerencia**: Ordenar por `MontoLimite` ascendente para mostrar opciones de menor a mayor

```csharp
public async Task<List<ConfiguracionCc>> GetAccountConfigsAsync()
{
    return await _context.ConfiguracionCcs
        .OrderBy(c => c.MontoLimite)  // Bronce → Platino
        .ToListAsync();
}
```

---

#### 2. Soft Delete Mejorado

**Agregar método para recuperar solo activas**:

```csharp
// Repository
public async Task<List<ConfiguracionCc>> GetActiveAccountConfigsAsync()
{
    return await _context.ConfiguracionCcs
        .Where(c => c.Activo == true)
        .OrderBy(c => c.MontoLimite)
        .ToListAsync();
}

// Service
public async Task<Result<List<AccountConfigDTO>>> GetActiveAccountConfigs()
{
    var result = await _accountConfigRepository.GetActiveAccountConfigsAsync();
    var dto = _mapper.Map<List<AccountConfigDTO>>(result);
    return Result<List<AccountConfigDTO>>.Succes(dto);
}

// Controller
[HttpGet("account-configs/active")]
public async Task<IActionResult> GetActiveAccountConfigs()
{
    var result = await _accountConfigService.GetActiveAccountConfigs();
    return Ok(result.Value);
}
```

**Uso**: Dropdown en módulo Cliente solo muestra configuraciones activas

---

### Bugs Encontrados

#### 1. Log Message Incorrecto

**Archivo**: `AccountConfigService.cs` (línea 112)

**Bug**:
```csharp
_logger.LogError("Error inesperado al creating la configuración: " + ex);
```

**Fix**:
```csharp
_logger.LogError("Error inesperado al actualizar la configuración: " + ex);
```

---

#### 2. Códigos de Error No Utilizados

**Archivo**: `AccountConfigErrorCode.cs`

**Códigos Definidos pero NO Usados**:
- `account_config_already_active`
- `account_config_already_inactive`
- `account_config_creation_failed`
- `account_config_update_failed`

**Sugerencia**:
- Eliminar si no se van a usar
- O implementar validaciones más específicas

---

### Consideraciones de Seguridad

#### 1. SQL Injection

**Estado**: ✅ **Protegido**
- EF Core usa parámetros parametrizados automáticamente
- No hay concatenación de strings SQL

---

#### 2. Mass Assignment

**Estado**: ⚠️ **Potencial riesgo menor**

**Problema**:
- Si se agregan campos sensibles al modelo en el futuro (ej: `EsConfiguracionSistema`)
- DTOs actuales previenen esto

**Mejor Práctica**: Mantener DTOs separados siempre

---

#### 3. Autorización a Nivel de Registro

**Problema Potencial**:
- Si se implementa multi-tenancy, falta validación de ownership

**Solución**: Agregar filtros de tenant al DbContext

---

## 📁 Ubicación de Archivos

### Estructura Completa del Módulo

```
C:\Users\enzo_\OneDrive\Documentos\Facultad\ProyectoFinal\venta_stock_webapi\
│
├── Models/
│   └── ConfiguracionCc.cs                          # Entidad EF Core
│
├── Data/
│   └── VentaStockContext.cs                        # DbContext (líneas 513-524)
│
├── CurrentAccount/
│   ├── Controllers/
│   │   └── CurrentAccountController.cs             # Endpoints API (AccountConfig)
│   │
│   ├── DTO/
│   │   └── AccountConfigDTO/
│   │       ├── AccountConfigDTO.cs                 # Response DTO
│   │       ├── CreateAccountConfigDTO.cs           # Create DTO
│   │       └── UpdateAccountConfigDTO.cs           # Update DTO
│   │
│   ├── Repository/
│   │   └── AccountConfigRepository/
│   │       ├── IAccountConfigRepository.cs         # Interface
│   │       └── AccountConfigRepository.cs          # Implementación
│   │
│   ├── Services/
│   │   └── AccountConfigService/
│   │       ├── IAccountConfigService.cs            # Interface
│   │       └── AccountConfigService.cs             # Lógica de negocio
│   │
│   ├── Profile/
│   │   └── CurrentAccountConfigProfile.cs          # AutoMapper mappings
│   │
│   └── Message/
│       └── AccountConfigErrorCode.cs               # Códigos y mensajes de error
│
├── Shared/
│   ├── ResultPattern/
│   │   └── ResultT.cs                              # Result<T> pattern
│   │
│   ├── MessageProvider/
│   │   └── MessageProvider.cs                      # Proveedor de mensajes
│   │
│   └── Paged/
│       └── PagedList.cs                            # Paginación (no usado aún)
│
└── Program.cs                                       # DI registration (líneas 48-54)
```

---

### Archivos Relacionados en Otros Módulos

**Client Module** (para referencia de integración):
```
Client/
├── DTO/
│   └── ClientCreateDTO.cs      # Usa LimiteCuenta (integración implícita)
└── Services/
    └── ClientService.cs        # Podría validar límite contra AccountConfig
```

---

## 📚 Referencias y Convenciones

### Convenciones de Nomenclatura

**Entidad**: `ConfiguracionCc` (PascalCase)

**Tabla DB**: `configuracion_cc` (snake_case)

**DTOs**:
- `AccountConfigDTO` (sufijo DTO)
- `CreateAccountConfigDTO` (prefijo + sufijo)
- `UpdateAccountConfigDTO` (prefijo + sufijo)

**Interfaces**:
- `IAccountConfigRepository` (prefijo I)
- `IAccountConfigService` (prefijo I)

**Implementaciones**:
- `AccountConfigRepository` (sin sufijo)
- `AccountConfigService` (sin sufijo)

---

### Patrones Utilizados

1. **Repository Pattern**: Abstracción de acceso a datos
2. **Result Pattern**: Manejo de resultados de operaciones
3. **DTO Pattern**: Separación de contratos API de modelos de dominio
4. **Dependency Injection**: Inversión de control
5. **AutoMapper**: Mapeo objeto-objeto
6. **Soft Delete**: `Activo` field en lugar de eliminación física

---

### Tecnologías y Versiones

- **.NET**: 8.0
- **EF Core**: 8.x
- **PostgreSQL**: (versión según Railway)
- **AutoMapper**: (versión según packages)

---

## 🎯 Resumen Ejecutivo para Desarrollo de UI

### Lo Más Importante

✅ **CRUD Completo**: Crear, Leer, Actualizar, Toggle Estado

✅ **5 Endpoints**:
1. GET all
2. GET by ID
3. POST create
4. PUT update
5. DELETE toggle-state

✅ **Validaciones Críticas**:
- Nombre único
- Monto límite único
- Ambos deben ser mayores a 0

✅ **Soft Delete**: Campo `Activo` (true/false)

✅ **Filtrado por Estado**: Query parameter `?activo=true|false` para filtrar configuraciones

✅ **Integración con Cliente**: Dropdown de configuraciones para seleccionar límite de CC

⚠️ **Limitaciones**:
- Sin paginación
- Sin autenticación
- Sin validación de uso en cascade

---

### Flujo de Usuario Típico

**Administrador**:
1. Accede a módulo "Configuración de Cuenta Corriente"
2. Ve tabla con todas las configuraciones (Bronze, Silver, Gold, etc.)
3. Puede crear nueva configuración (ej: "Platinum - $1,000,000")
4. Puede editar nombre o monto de configuración existente
5. Puede activar/desactivar configuraciones

**Vendedor (al crear cliente)**:
1. Marca checkbox "Tiene Cuenta Corriente"
2. Aparece dropdown con configuraciones activas
3. Selecciona "Silver - $100,000"
4. Sistema asigna `limite_cuenta = 100000` al cliente

---

### Datos de Ejemplo para Testing

**Configuraciones Base**:

```json
[
  { "nombre": "Bronze", "montoLimite": 50000 },
  { "nombre": "Silver", "montoLimite": 100000 },
  { "nombre": "Gold", "montoLimite": 500000 },
  { "nombre": "Platinum", "montoLimite": 1000000 },
  { "nombre": "Diamond", "montoLimite": 5000000 }
]
```

**Casos de Prueba**:

✅ **Crear configuración válida**: "Emerald - $250,000"

❌ **Crear con nombre duplicado**: "Bronze" (ya existe)

❌ **Crear con límite duplicado**: $100,000 (ya usado por Silver)

✅ **Actualizar nombre**: "Bronze" → "Bronze Premium"

✅ **Actualizar monto**: $50,000 → $75,000

❌ **Actualizar a nombre existente**: "Bronze Premium" → "Silver" (existe)

✅ **Desactivar configuración**: Bronze (Activo=true → false)

✅ **Reactivar configuración**: Bronze (Activo=false → true)

---

## 🏁 Conclusión

Esta documentación proporciona una **visión completa y detallada** de la implementación del módulo **AccountConfig**, incluyendo:

- ✅ Arquitectura y estructura de archivos
- ✅ Especificación de base de datos
- ✅ DTOs y validaciones
- ✅ Lógica de negocio completa
- ✅ Endpoints de API con ejemplos
- ✅ Integración con otros módulos
- ✅ Sugerencias de mejoras
- ✅ Guía para desarrollo de UI

**Úsala como referencia** para:
1. Desarrollar la interfaz de usuario en React
2. Entender el flujo completo de datos
3. Implementar validaciones en frontend
4. Identificar mejoras necesarias
5. Documentar otros módulos del sistema

---

**Versión**: 1.0
**Última Actualización**: 2025-12-12
**Documentado por**: Claude Code (Exploration Agent)
**Para**: Enzo (Desarrollador Backend)
