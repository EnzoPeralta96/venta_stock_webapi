# Módulo de Clientes

> Documentación completa del módulo `Features/Client`.

---

## 1. Descripción General

El módulo de Clientes gestiona el ciclo de vida de los clientes del sistema: alta, consulta, búsqueda paginada, actualización y baja/reactivación lógica.
Soporta dos tipos de cliente: **Persona Física** y **Empresa**, con validaciones diferenciadas en cada caso. Además, permite la creación opcional de una **Cuenta Corriente** al momento del alta.

---

## 2. Modelo de Dominio — `Cliente`

**Archivo:** `Models/Cliente.cs`

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IdCliente` | `int` | PK autoincremental |
| `Nombre` | `string?` | Nombre (obligatorio para persona física) |
| `Apellido` | `string?` | Apellido (obligatorio para persona física) |
| `RazonSocial` | `string?` | Razón social (obligatorio para empresa) |
| `Cuit` | `string?` | CUIT (obligatorio para empresa, puede repetirse) |
| `Dni` | `string?` | DNI (obligatorio para persona física, único) |
| `Telefono` | `string?` | Teléfono de contacto |
| `Mail` | `string?` | Correo electrónico (único global) |
| `FechaAlta` | `DateOnly?` | Fecha de creación del cliente |
| `FechaBaja` | `DateOnly?` | Fecha de baja lógica (`null` = activo) |

### Relaciones de navegación

| Propiedad | Tipo | Relación |
|---|---|---|
| `MovimientoCcs` | `ICollection<MovimientoCc>` | Movimientos de cuenta corriente del cliente |
| `Venta` | `ICollection<Ventum>` | Ventas asociadas al cliente |

---

## 3. Arquitectura de Capas

```
Controllers/ClienteController.cs
    │
    ▼
Services/ClientService.cs  ◄── IClientService.cs
    │
    ▼
Repository/ClientRepository.cs  ◄── IClientRepository.cs
    │
    ▼
Data/VentaStockContext (EF Core)
```

---

## 4. Controller — `ClienteController`

**Archivo:** `Features/Client/Controllers/ClienteController.cs`
**Ruta base:** `api/Cliente`
**Autorización:** Requiere autenticación (`[Authorize]`) + policies por endpoint.

### Endpoints

| Método | Ruta | Policy | Acción | Request DTO | Response |
|---|---|---|---|---|---|
| `POST` | `create` | `PERM:CLI_CREATE` | Crear cliente | `ClientCreateDTO` (body) | `ClientResponseDTO` |
| `GET` | `client/{id}` | `PERM:CLI_READ` | Obtener cliente por ID | `int id` (ruta) | `ClientResponseDTO` |
| `GET` | `search` | `PERM:CLI_READ` | Buscar clientes paginados | QueryString: `pageIndex`, `pageSize`, `searchTerm`, `estado` | `PagedList<ClientResponseDTO>` |
| `PUT` | `update` | `PERM:CLI_UPDATE` | Actualizar cliente | `ClientUpdateDTO` (body) | `ClientResponseDTO` |
| `PUT` | `toggle-status` | `PERM:CLI_DELETE` | Activar/desactivar cliente | `ClientToggleStatusDTO` (body) | `string` (mensaje) |

### Patrón de respuesta

Todos los endpoints utilizan el patrón `Result<T>`:
- **Éxito:** retorna `Ok(result.Value)`
- **Error:** convierte el `ErrorCode` a mensaje legible mediante `MessageProvider.Get(...)` y retorna `BadRequest` o `NotFound`.

---

## 5. Service — `ClientService`

**Archivo:** `Features/Client/Services/ClientService.cs`
**Interfaz:** `IClientService.cs`

### Dependencias inyectadas

| Dependencia | Uso |
|---|---|
| `IClientRepository` | Acceso a datos de clientes |
| `IAccountMovementRepository` | Creación del movimiento inicial de CC |
| `VentaStockContext` | Transacciones explícitas |
| `IMapper` | Mapeo DTO ↔ Entidad |
| `ILogger<ClientService>` | Logging |
| `IUserContext` | Contexto del usuario autenticado |

### Métodos y Lógica de Negocio

#### `CreateClienteAsync(ClientCreateDTO dto)`
1. Inicia transacción.
2. **Valida unicidad de Email** (global para todos los clientes activos).
3. Si `EsEmpresa`:
   - Valida unicidad de `RazonSocial`.
   - El CUIT **no** se valida por unicidad (puede repetirse).
4. Si **persona física**:
   - Valida unicidad de `Dni`.
5. Si `TieneCuentaCorriente`:
   - Requiere que `LimiteCuenta` tenga valor.
   - Crea un `MovimientoCc` inicial con tipo `ALTA_CLIENTE` (id=2), estado `Aprobado` (id=2), saldo inicial configurable.
6. Commit y retorna el cliente creado.

#### `GetClient(int id)`
- Busca cliente por ID incluyendo sus `MovimientoCcs`.
- Retorna `Failure` si no existe.

#### `Search(int pageIndex, int pageSize, string searchTerm, string estado)`
- Filtra por: `Nombre`, `Apellido`, `RazonSocial`, `Dni`, `Cuit`, `Mail`, `Telefono`.
- Filtra por estado: `"activos"` (FechaBaja == null), `"eliminados"` (FechaBaja != null), u otro valor para todos.
- Retorna lista paginada (`PagedList<ClientResponseDTO>`).

#### `UpdateClient(ClientUpdateDTO dto)`
1. Inicia transacción.
2. Verifica existencia del cliente.
3. Valida unicidad de Email, RazonSocial o DNI excluyendo al cliente actual.
4. Actualiza campos directamente en la entidad existente.
5. Commit y retorna el cliente actualizado.

#### `ToggleStatus(ClientToggleStatusDTO dto)`
1. Inicia transacción.
2. Verifica existencia del cliente.
3. Si `!IsActive` → da de baja (asigna `FechaBaja = hoy`). Valida que no esté ya dado de baja.
4. Si `IsActive` → reactiva (pone `FechaBaja = null`). Valida que no esté ya activo.
5. Registra auditoría (`AuditDbSession.SetAsync`).

---

## 6. Repository — `ClientRepository`

**Archivo:** `Features/Client/Repository/ClientRepository.cs/ClientRepository.cs`
**Interfaz:** `IClientRepository.cs`

### Métodos

| Método | Descripción |
|---|---|
| `DniExistsAsync(string dni)` | Verifica si existe un cliente activo con ese DNI |
| `CuitExistsAsync(string cuit)` | Verifica si existe un cliente activo con ese CUIT |
| `EmailExistsAsync(string email)` | Verifica si existe un cliente activo con ese email |
| `EnterpriseExistsAsync(string enterprise)` | Verifica si existe un cliente activo con esa razón social |
| `DniExistsForOtherClientAsync(string, int)` | Unicidad de DNI excluyendo un cliente dado |
| `CuitExistsForOtherClientAsync(string, int)` | Unicidad de CUIT excluyendo un cliente dado |
| `EmailExistsForOtherClientAsync(string, int)` | Unicidad de email excluyendo un cliente dado |
| `EnterpriseExistsForOtherClientAsync(string, int)` | Unicidad de razón social excluyendo un cliente dado |
| `ExistsByIdAsync(int idCliente)` | Verifica existencia por ID |
| `GetByIdAsync(int idCliente)` | Obtiene cliente por ID con `Include(MovimientoCcs)` |
| `CreateAsync(Cliente)` | Crea y persiste un nuevo cliente |
| `UpdateAsync(Cliente)` | Actualiza un cliente existente |
| `UpdateStatusAsync(int, DateOnly?)` | Actualiza solo `FechaBaja` con `ExecuteUpdateAsync` |
| `ClientsQueryable(string searchTerm)` | Retorna `IQueryable` filtrado por término de búsqueda |
| `ObtenerInfoCreditoAsync(int idCliente)` | Obtiene saldo y límite del último movimiento CC |

### Clase auxiliar — `CreditInfo`

```csharp
public class CreditInfo
{
    public decimal SaldoActual { get; set; }
    public decimal LimiteCuenta { get; set; }
    public decimal LimiteDisponible => LimiteCuenta - SaldoActual;
    public decimal PorcentajeUso => LimiteCuenta > 0 ? (SaldoActual / LimiteCuenta) * 100 : 0;
}
```

Definida en `IClientRepository.cs`, retornada por `ObtenerInfoCreditoAsync`. Usada para consultar la información crediticia de un cliente.

---

## 7. DTOs

### `ClientCreateDTO`

**Archivo:** `Features/Client/DTO/ClientCreateDTO.cs`
Implementa `IValidatableObject` para validaciones condicionales.

| Campo | Tipo | Regla |
|---|---|---|
| `Nombre` | `string` | Obligatorio si `EsEmpresa == false` |
| `Apellido` | `string` | Obligatorio si `EsEmpresa == false` |
| `Dni` | `string` | Obligatorio si `EsEmpresa == false` |
| `EsEmpresa` | `bool` | Discriminador de tipo de cliente |
| `RazonSocial` | `string` | Obligatorio si `EsEmpresa == true` |
| `Cuit` | `string` | Obligatorio si `EsEmpresa == true` |
| `Telefono` | `string` | Siempre obligatorio (`[Required]`) |
| `Mail` | `string` | Siempre obligatorio (`[Required]`, `[EmailAddress]`) |
| `TieneCuentaCorriente` | `bool` | Si `true`, se crea CC al dar de alta |
| `LimiteCuenta` | `decimal?` | Obligatorio y > 0 si `TieneCuentaCorriente == true` |
| `SaldoInicial` | `decimal?` | Opcional, no puede ser negativo |
| `idUsuarioRegistra` | `int` | ID del usuario que registra |

### `ClientResponseDTO`

**Archivo:** `Features/Client/DTO/ClientResponseDTO.cs`

| Campo | Tipo | Descripción |
|---|---|---|
| `IdCliente` | `int` | ID del cliente |
| `Nombre` | `string` | Nombre |
| `Apellido` | `string` | Apellido |
| `RazonSocial` | `string` | Razón social |
| `Dni` | `string` | DNI |
| `Cuit` | `string` | CUIT |
| `Telefono` | `string` | Teléfono |
| `Mail` | `string` | Email |
| `EsEmpresa` | `bool` | Calculado en el mapeo (RazonSocial y Cuit no vacíos) |
| `TieneCuentaCorriente` | `bool` | Calculado en el mapeo (tiene MovimientoCcs) |

### `ClientUpdateDTO`

**Archivo:** `Features/Client/DTO/ClientUpdateDTO.cs`
Implementa `IValidatableObject`. Mismas reglas condicionales que `ClientCreateDTO` más validación `IdCliente > 0`.

### `ClientToggleStatusDTO`

**Archivo:** `Features/Client/DTO/ClientToggleStatusDTO.cs`

| Campo | Tipo | Regla |
|---|---|---|
| `IdCliente` | `int` | Obligatorio |
| `IsActive` | `bool` | `true` = reactivar, `false` = dar de baja |

---

## 8. AutoMapper Profile — `ClientProfile`

**Archivo:** `Features/Client/Profile/ClientProfile.cs`

| Mapeo | Lógica destacada |
|---|---|
| `Cliente → ClientResponseDTO` | `TieneCuentaCorriente` = `src.MovimientoCcs.Any()`, `EsEmpresa` = `RazonSocial` y `Cuit` no vacíos |
| `ClientCreateDTO → Cliente` | Mapeo directo |
| `ClientUpdateDTO → Cliente` | Mapeo directo |

---

## 9. Códigos de Error — `ClientErrorCode`

**Archivo:** `Features/Client/Message/ClientErrorCode.cs`

| Código | Mensaje |
|---|---|
| `cliente_not_found` | El cliente indicado no existe. |
| `dni_in_use` | El DNI ya está registrado. |
| `cuit_in_use` | El CUIT ya está registrado. |
| `email_in_use` | El correo electrónico ya está en uso. |
| `invalid_persona_fisica_data` | Para persona física se requiere DNI, Nombre y Apellido. |
| `invalid_empresa_data` | Para empresa se requiere CUIT y Razón Social. |
| `limite_cuenta_required` | El límite de cuenta es obligatorio cuando se crea una cuenta corriente. |
| `configuracion_cc_not_found` | No se encontró la configuración de cuenta corriente. |
| `cliente_already_active` | El cliente ya está activo. |
| `cliente_already_inactive` | El cliente ya está dado de baja. |
| `unexpected_error` | Ocurrió un error inesperado, por favor intente nuevamente. |
| `empresa_in_use` | La razón social ya está en uso. |

---

## 10. Resumen de Archivos del Módulo

```
Features/Client/
├── Controllers/
│   └── ClienteController.cs          # 5 endpoints REST
├── DTO/
│   ├── ClientCreateDTO.cs            # DTO de creación con validaciones condicionales
│   ├── ClientResponseDTO.cs          # DTO de respuesta
│   ├── ClientToggleStatusDTO.cs      # DTO para activar/desactivar
│   └── ClientUpdateDTO.cs            # DTO de actualización con validaciones
├── Message/
│   └── ClientErrorCode.cs            # Enum + diccionario de errores
├── Profile/
│   └── ClientProfile.cs              # Perfiles de AutoMapper
├── Repository/
│   └── ClientRepository.cs/
│       ├── IClientRepository.cs      # Interfaz + CreditInfo
│       └── ClientRepository.cs       # Implementación con EF Core
├── Services/
│   ├── IClientService.cs             # Interfaz del servicio
│   └── ClientService.cs              # Lógica de negocio
```
