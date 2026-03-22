# Módulo de Cuenta Corriente

> Documentación completa del módulo `Features/CurrentAccount`.

---

## 1. Descripción General

El módulo de Cuenta Corriente gestiona las cuentas corrientes de los clientes, permitiendo:
- **Crear** una cuenta corriente para un cliente existente.
- **Registrar movimientos**: ventas a crédito, pagos (globales y por factura), notas de débito/crédito, intereses.
- **Consultar** el historial de movimientos y ventas pendientes de pago.
- **Generar comprobantes PDF** de pagos realizados.
- **Configurar** plantillas de límites de cuenta predefinidos.

Utiliza un **Strategy Pattern** para calcular el impacto de cada tipo de movimiento sobre el saldo y el límite disponible.

---

## 2. Modelos de Dominio

### 2.1 `MovimientoCc` — Movimiento de Cuenta Corriente

**Archivo:** `Models/MovimientoCc.cs`

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IdMovimiento` | `int` | PK autoincremental |
| `Importe` | `decimal?` | Monto del movimiento |
| `Fecha` | `DateTime?` | Fecha/hora del movimiento |
| `Detalle` | `string?` | Descripción textual del movimiento |
| `IdEstado` | `int?` | FK → `Estado` (ej. 2 = Aprobado) |
| `SaldoActual` | `decimal?` | Saldo de la cuenta **después** de aplicar el movimiento |
| `LimiteCuenta` | `decimal?` | Límite disponible **después** de aplicar el movimiento |
| `IdTipoMovimiento` | `int?` | FK → `TipoMovimiento` |
| `FechaAutorizacion` | `DateTime?` | Fecha de autorización (si aplica) |
| `IdUsuarioAutoriza` | `int?` | FK → `Usuario` autorizador |
| `IdVenta` | `int?` | FK → `Ventum` (solo para movimientos vinculados a una venta) |
| `IdCliente` | `int?` | FK → `Cliente` |
| `IdUsuarioRegistra` | `int?` | FK → `Usuario` que registra |
| `MontoPagado` | `decimal?` | Solo para movimientos tipo `MOVIMIENTO_CC` (consumo): acumula el total pagado contra este consumo. Permite derivar `EstadoPago`: pendiente / parcial / pagado |

#### Relaciones de navegación

| Propiedad | Tipo |
|---|---|
| `IdClienteNavigation` | `Cliente?` |
| `IdEstadoNavigation` | `Estado?` |
| `IdTipoMovimientoNavigation` | `TipoMovimiento?` |
| `IdUsuarioAutorizaNavigation` | `Usuario?` |
| `IdUsuarioRegistraNavigation` | `Usuario?` |
| `IdVentaNavigation` | `Ventum?` |

---

### 2.2 `TipoMovimiento`

**Archivo:** `Models/TipoMovimiento.cs`

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IdMovimiento` | `int` | PK (nota: el nombre real del campo es `IdTipoMovimiento` en la tabla) |
| `Nombre` | `string?` | Nombre del tipo (ej. "Pago Global") |
| `Accion` | `string?` | Descripción de la acción |
| `MovimientoCcs` | `ICollection<MovimientoCc>` | Relación inversa |

#### Tipos de movimiento (Enum `TypeMovement`)

| Enum | ID | Efecto sobre saldo | Efecto sobre límite |
|---|---|---|---|
| `ALTA_CLIENTE` | 2 | — | — |
| `NOTA_DEBITO` | 3 | Sube (+ importe) | Baja (- importe) |
| `NOTA_CREDITO` | 4 | Baja (- importe) | Sube (+ importe) |
| `MOVIMIENTO_CC` | 5 | Sube (+ importe) | Baja (- importe) |
| `PAGO_GLOBAL` | 6 | Baja (- importe) | Sube (+ importe) |
| `INTERES_SALDO_GLOBAL` | 7 | Sube (+ importe) | Baja (- importe) |
| `PAGO_FACTURA` | 8 | Baja (- importe) | Sube (+ importe) |

> **Nota:** Los tipos `ALTA_CLIENTE` (2) y `MOVIMIENTO_CC` (5) se excluyen del listado de tipos disponibles en `GetMovementType()` ya que no son seleccionables directamente por el usuario.

---

### 2.3 `ConfiguracionCc` — Configuración de Cuenta Corriente

**Archivo:** `Models/ConfiguracionCc.cs`

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IdConfig` | `int` | PK autoincremental |
| `Nombre` | `string` | Nombre de la configuración (único) |
| `MontoLimite` | `decimal` | Monto límite predefinido (único) |
| `Activo` | `bool` | Si la configuración está activa (default: `true`) |

---

### 2.4 `Estado`

**Archivo:** `Models/Estado.cs`

| Propiedad | Tipo | Descripción |
|---|---|---|
| `IdEstado` | `int` | PK |
| `Estado1` | `string?` | Nombre del estado (ej. "Aprobado") |

---

## 3. Arquitectura de Capas

```
Controllers/
├── CurrentAccountController.cs     (movimientos)
└── AccountConfigController.cs      (configuraciones)
    │
    ▼
Services/
├── CurrentAccountService/
│   ├── CurrentAccountService.cs  ◄── ICurrentAccountService.cs
│   └── StrategyCurrentAccount/
│       ├── IMovementStrategy.cs
│       ├── MovementStrategy.cs       (5 estrategias concretas)
│       ├── MovementStrategyFactory.cs
│       └── TypeMovements.cs          (Enum)
└── AccountConfigService/
    └── AccountConfigService.cs  ◄── IAccountConfigService.cs
    │
    ▼
Repository/
├── AccountMovementRepository/
│   ├── AccountMovementRepository.cs  ◄── IAccountMovementRepository.cs
└── AccountConfigRepository/
    └── AccountConfigRepository.cs  ◄── IAccountConfigRepository.cs
    │
    ▼
Data/VentaStockContext (EF Core)
```

---

## 4. Controllers

### 4.1 `CurrentAccountController`

**Archivo:** `Features/CurrentAccount/Controllers/CurrentAccountController.cs`
**Ruta base:** `api/CurrentAccount`
**Autorización:** `[Authorize]` + policies por endpoint.

| Método | Ruta | Policy | Acción | Request | Response |
|---|---|---|---|---|---|
| `GET` | `movements/{clientId}` | `PERM:CC_VIEW` | Obtener historial de movimientos | `int clientId` (ruta) | `List<AccountMovementDTO>` |
| `POST` | `create-account` | `PERM:CLI_CREATE` | Crear CC para un cliente existente | `CreateCurrentAccountDTO` (body) | `201 Created` |
| `GET` | `movement-types` | `PERM:CC_VIEW` | Listar tipos de movimiento disponibles | — | `List<TypeMovementDTO>` |
| `POST` | `register-movement` | `PERM:CC_REGISTER_PAYMENT` | Registrar un movimiento (pago, ND, NC, etc.) | `AddMovementDTO` (body) | `{ idMovimiento: int }` |
| `GET` | `pending-sales/{clientId}` | `PERM:CC_VIEW` | Obtener ventas con saldo pendiente | `int clientId` (ruta) | `List<PendingSalePaymentDTO>` |
| `GET` | `payment-receipt/{idMovimiento}` | `PERM:CC_VIEW` | Descargar comprobante PDF de pago | `int idMovimiento` (ruta) | `application/pdf` |

---

### 4.2 `AccountConfigController`

**Archivo:** `Features/CurrentAccount/Controllers/AccountConfigController.cs`
**Ruta base:** `api/AccountConfig`
**Autorización:** `[Authorize]` + policies por endpoint.

| Método | Ruta | Policy | Acción | Request | Response |
|---|---|---|---|---|---|
| `GET` | `account-configs/{configId}` | `PERM:CC_VIEW` | Obtener configuración por ID | `int configId` (ruta) | `AccountConfigDTO` |
| `GET` | `account-configs` | `PERM:CC_VIEW` | Listar configuraciones (filtro opcional por activo) | `?activo=true/false` (query) | `List<AccountConfigDTO>` |
| `POST` | `create-account-configs` | `PERM:CC_MANAGE` | Crear nueva configuración | `CreateAccountConfigDTO` (body) | `string` |
| `PUT` | `update-account-configs` | `PERM:CC_MANAGE` | Actualizar configuración | `UpdateAccountConfigDTO` (body) | `string` |
| `DELETE` | `toggle-state/{configId}/{active}` | `PERM:CC_MANAGE` | Activar/desactivar configuración | `int`, `bool` (ruta) | `string` |

---

## 5. Services

### 5.1 `CurrentAccountService`

**Archivo:** `Features/CurrentAccount/Services/CurrentAccountService/CurrentAccountService.cs`
**Interfaz:** `ICurrentAccountService.cs`

#### Dependencias inyectadas

| Dependencia | Uso |
|---|---|
| `IAccountMovementRepository` | Acceso a datos de movimientos CC |
| `IClientRepository` | Verificación de existencia de clientes |
| `IFerreteriaRepository` | Datos de la ferretería para el comprobante PDF |
| `IMapper` | Mapeo DTO ↔ Entidad |
| `ILogger` | Logging |
| `MovementStrategyFactory` | Obtiene la estrategia de cálculo según tipo de movimiento |

#### Métodos y Lógica de Negocio

##### `GetAccountMovementsByClientId(int clientId)`
1. Verifica que el cliente exista.
2. Obtiene todos los movimientos del cliente ordenados cronológicamente.
3. Mapea a `List<AccountMovementDTO>`.

##### `CreateAccountMovement(CreateCurrentAccountDTO dto)`
1. Verifica existencia del cliente.
2. Mapea el DTO a `MovimientoCc`.
3. Completa el `Detalle` obteniendo la acción del tipo de movimiento.
4. Persiste el movimiento (tipo `ALTA_CLIENTE`, estado `Aprobado`).

##### `RegisterMovement(AddMovementDTO dto)` ⭐
Método principal para registrar cualquier tipo de movimiento. Flujo:
1. Obtiene el **último movimiento** del cliente (para tener el saldo y límite actuales).
2. Si no existe → error `account_not_found`.
3. Obtiene la **estrategia** correspondiente al tipo de movimiento vía `MovementStrategyFactory`.
4. Ejecuta `strategy.Calculate(balanceBase, limitBase, importe)` → obtiene nuevo saldo y límite.
5. Crea el movimiento con el saldo/límite calculados.
6. **Post-proceso de pagos:**
   - Si es `PAGO_FACTURA` con `IdVenta`: ejecuta `AllocarPagoFactura`. Suma el importe pagado al `MontoPagado` del consumo vinculado (capped al importe del consumo).
   - Si es `PAGO_GLOBAL`: ejecuta `AllocarPagoGlobal`. Distribuye el importe entre consumos impagos en orden cronológico (FIFO), cancelando deudas completas hasta agotar el monto pagado.
7. Retorna el `IdMovimiento` del nuevo registro para que el frontend solicite el comprobante PDF.

##### `GetMovementTypes()`
Obtiene la lista de tipos de movimiento disponibles (excluyendo `ALTA_CLIENTE` id=2 y `MOVIMIENTO_CC` id=5).

##### `GetPendingSalesForPayment(int clientId)`
1. Verifica existencia del cliente.
2. Si el saldo actual es ≤ 0, retorna lista vacía (no hay deuda).
3. Calcula ventas pendientes de pago: ventas con `IdMedioPago = 2` (cuenta corriente) y `IdEstado = 2` (aprobadas), restando los pagos realizados de tipo `PAGO_FACTURA` (id=8).
4. Solo retorna ventas con `SaldoPendiente > 0`.

##### `GeneratePaymentReceiptAsync(int idMovimiento)`
1. Obtiene el movimiento por ID con todas sus navegaciones.
2. Obtiene datos de la ferretería.
3. Construye el `PaymentReceiptDataSource` con datos del cliente, pago, venta asociada, y usuario que registró.
4. Genera el PDF usando **QuestPDF** y retorna los bytes.

#### Métodos privados de asignación de pagos

##### `AllocarPagoFactura(int idVenta, decimal importePago)`
- Busca el consumo (`MOVIMIENTO_CC`) vinculado a la venta.
- Suma el importe pagado a `MontoPagado`, con tope en el importe del consumo.

##### `AllocarPagoGlobal(int clientId, decimal importePago)`
- Obtiene consumos impagos ordenados cronológicamente (FIFO).
- Distribuye el pago secuencialmente: cancela deudas completas hasta agotar el monto; el último consumo puede quedar en estado parcial.

---

### 5.2 `AccountConfigService`

**Archivo:** `Features/CurrentAccount/Services/AccountConfigService/AccountConfigService.cs`
**Interfaz:** `IAccountConfigService.cs`

#### Métodos

| Método | Lógica |
|---|---|
| `GetAccountConfigById(int)` | Obtiene configuración por ID, retorna error si no existe |
| `GetAccountConfigs(bool? activo)` | Lista configuraciones con filtro opcional por estado activo |
| `CreateAccountConfig(CreateAccountConfigDTO)` | Valida unicidad de nombre y límite, crea la config (activa por defecto) |
| `UpdateAccountConfig(UpdateAccountConfigDTO)` | Valida unicidad de nombre y límite excluyendo la config actual, actualiza nombre y monto |
| `ToggleStateAccountConfig(int, bool)` | Verifica existencia, cambia estado activo/inactivo |

---

## 6. Strategy Pattern — Cálculo de Movimientos

### 6.1 Interfaz y Resultado

**Archivo:** `StrategyCurrentAccount/IMovementStrategy.cs`

```csharp
public record CalculationResult(decimal NewBalance, decimal NewLimit);
public interface IMovementStrategy
{
    CalculationResult Calculate(decimal oldBalance, decimal oldLimit, decimal amount);
}
```

### 6.2 Estrategias Concretas

**Archivo:** `StrategyCurrentAccount/MovementStrategy.cs`

| Estrategia | Usada por | Fórmula Saldo | Fórmula Límite | Notas |
|---|---|---|---|---|
| `SaleStrategy` | `MOVIMIENTO_CC` | `+amount` | `-amount` | Lanza `InvalidOperationException` si el nuevo límite < 0 |
| `PaymentStrategy` | `PAGO_GLOBAL`, `PAGO_FACTURA` | `-amount` | `+amount` | — |
| `InterestStrategy` | `INTERES_SALDO_GLOBAL` | `+amount` | `-amount` | Tratado igual que nota de débito |
| `DebitNoteStrategy` | `NOTA_DEBITO` | `+amount` | `-amount` | TODO: controlar cuando el límite es 0 |
| `CreditNoteStrategy` | `NOTA_CREDITO` | `-amount` | `+amount` | TODO: controlar si es bonificación o devolución de productos |

### 6.3 Factory

**Archivo:** `StrategyCurrentAccount/MovementStrategyFactory.cs`

```csharp
public IMovementStrategy GetStrategy(TypeMovement typeMovement)
{
    return typeMovement switch
    {
        TypeMovement.MOVIMIENTO_CC         => new SaleStrategy(),
        TypeMovement.PAGO_GLOBAL           => new PaymentStrategy(),
        TypeMovement.PAGO_FACTURA          => new PaymentStrategy(),
        TypeMovement.INTERES_SALDO_GLOBAL  => new InterestStrategy(),
        TypeMovement.NOTA_DEBITO           => new DebitNoteStrategy(),
        TypeMovement.NOTA_CREDITO          => new CreditNoteStrategy(),
        _ => throw new NotSupportedException(...)
    };
}
```

---

## 7. Repositories

### 7.1 `AccountMovementRepository`

**Archivo:** `Features/CurrentAccount/Repository/AccountMovementRepository/AccountMovementRepository.cs`
**Interfaz:** `IAccountMovementRepository.cs`

| Método | Descripción |
|---|---|
| `CreateMovement(MovimientoCc)` | Persiste un nuevo movimiento |
| `GetMovements(int clientId)` | Obtiene todos los movimientos del cliente con Include de Estado, TipoMovimiento, UsuarioRegistra, Venta→Estado. Ordenados por Fecha ASC, luego por IdMovimiento ASC |
| `GetLastMovement(int clientId)` | Obtiene el último movimiento del cliente (por Fecha DESC, IdMovimiento DESC). Usado para obtener saldo/límite actuales |
| `GetDetailMovement(int tipoMov)` | Obtiene el campo `Accion` del tipo de movimiento |
| `GetMovementType()` | Lista tipos de movimiento excluyendo IDs 2 (ALTA_CLIENTE) y 5 (MOVIMIENTO_CC) |
| `GetPendingSalesForPayment(int clientId)` | Consulta ventas pendientes de pago (detallado abajo) |
| `GetUnpaidConsumptions(int clientId)` | Consumos (tipo 5) con `MontoPagado < Importe`, ordenados cronológicamente. Usado por `AllocarPagoGlobal` |
| `GetConsumptionByVentaId(int idVenta)` | Busca el consumo (tipo 5) vinculado a una venta. Usado por `AllocarPagoFactura` |
| `GetMovementById(int idMovimiento)` | Obtiene un movimiento con Include de Cliente, TipoMovimiento, UsuarioRegistra y Venta. Usado para generar PDF |
| `UpdateMovimientos(List<MovimientoCc>)` | Actualiza `MontoPagado` en batch |

#### Consulta de ventas pendientes (`GetPendingSalesForPayment`)

```
Ventas CC (IdMedioPago = 2) aprobadas (IdEstado = 2)
    LEFT JOIN MovimientoCc tipo PAGO_FACTURA (id = 8)
    → TotalPagado = SUM(pagos por factura)
    → SaldoPendiente = TotalVenta - TotalPagado
    → Solo retorna SaldoPendiente > 0
    → Orden: Fecha DESC
```

---

### 7.2 `AccountConfigRepository`

**Archivo:** `Features/CurrentAccount/Repository/AccountConfigRepository/AccountConfigRepository.cs`
**Interfaz:** `IAccountConfigRepository.cs`

| Método | Descripción |
|---|---|
| `GetAccountConfigByIdAsync(int)` | Obtiene configuración por ID |
| `GetAccountConfigsAsync(bool?)` | Lista configuraciones, filtro opcional por `Activo`. Orden: `MontoLimite` ASC |
| `CreateAccountConfigAsync(ConfiguracionCc)` | Crea nueva configuración |
| `UpdateAccountConfigAsync(ConfiguracionCc)` | Actualiza nombre y monto con `ExecuteUpdateAsync` |
| `ToggleStateAccountConfigAsync(int, bool)` | Cambia campo `Activo` con `ExecuteUpdateAsync` |
| `AccountConfigExistsByNameAsync(string)` | Verifica unicidad de nombre (creación) |
| `AccountConfigExistsByNameAsync(int, string)` | Verifica unicidad de nombre excluyendo un ID (actualización) |
| `AccountConfigExistsByLimitAsync(decimal)` | Verifica unicidad de monto límite (creación) |
| `AccountConfigExistsByLimitAsync(int, decimal)` | Verifica unicidad de monto límite excluyendo un ID (actualización) |

---

## 8. DTOs

### 8.1 DTOs de Movimientos

#### `AccountMovementDTO` — Respuesta de movimiento

| Campo | Tipo | Descripción |
|---|---|---|
| `IdMovimiento` | `int` | ID del movimiento |
| `TipoMovimiento` | `string` | Nombre del tipo de movimiento |
| `Detalle` | `string` | Detalle textual |
| `Estado` | `string` | Nombre del estado |
| `Fecha` | `DateTime` | Fecha/hora del movimiento |
| `Importe` | `decimal` | Monto |
| `SaldoActual` | `decimal?` | Saldo después del movimiento |
| `LimiteCuenta` | `decimal?` | Límite después del movimiento |
| `UsuarioRegistra` | `string` | Nombre completo del usuario que registra |
| `IdVenta` | `int?` | ID de venta asociada |
| `IdCliente` | `int?` | ID del cliente |
| `CodigoVenta` | `string` | Código de la venta asociada |
| `FechaVenta` | `DateTime?` | Fecha de la venta |
| `TotalVenta` | `decimal?` | Total de la venta |
| `EstadoVenta` | `string` | Estado de la venta |
| `MontoPagado` | `decimal?` | Monto pagado hasta ahora (solo tipo MOVIMIENTO_CC) |
| `EstadoPago` | `string` | Estado de pago derivado: `"pendiente"` / `"parcial"` / `"pagado"` / `null` |

#### `AddMovementDTO` — Registrar movimiento

Implementa `IValidatableObject`.

| Campo | Tipo | Validación |
|---|---|---|
| `IdCliente` | `int` | `[Required]` |
| `Importe` | `decimal` | `[Required]`, debe ser > 0 |
| `Detalle` | `string` | `[Required]` |
| `IdTipoMovimiento` | `int` | `[Required]` |
| `IdVenta` | `int?` | Obligatorio si `IdTipoMovimiento` es 5 (MOVIMIENTO_CC) u 8 (PAGO_FACTURA) |
| `IdUsuarioRegistra` | `int` | `[Required]`, default = 1 |

#### `CreateCurrentAccountDTO` — Crear cuenta corriente

Implementa `IValidatableObject`.

| Campo | Tipo | Validación |
|---|---|---|
| `Detalle` | `string` | `[Required]` |
| `LimiteCuenta` | `decimal` | `[Required]` |
| `IdCliente` | `int` | `[Required]` |
| `IdUsuarioRegistra` | `int` | `[Required]` |
| `TieneDueda` | `bool` | Si es `true`, `SaldoActual` debe ser > 0 |
| `SaldoActual` | `decimal` | Saldo inicial (obligatorio si `TieneDueda`) |

#### `PendingSalePaymentDTO` — Venta pendiente de pago

| Campo | Tipo | Descripción |
|---|---|---|
| `IdVenta` | `int` | ID de la venta |
| `CodigoVenta` | `string` | Código de la venta |
| `Fecha` | `DateTime?` | Fecha de la venta |
| `TotalVenta` | `decimal` | Monto total de la venta |
| `TotalPagado` | `decimal` | Acumulado de pagos realizados |
| `SaldoPendiente` | `decimal` | TotalVenta - TotalPagado |

#### `TypeMovementDTO` — Tipo de movimiento

| Campo | Tipo |
|---|---|
| `IdTipoMovimiento` | `int` |
| `Nombre` | `string` |
| `Accion` | `string` |

---

### 8.2 DTOs de Configuración

#### `AccountConfigDTO` — Respuesta

| Campo | Tipo |
|---|---|
| `IdConfig` | `int` |
| `Nombre` | `string` |
| `MontoLimite` | `decimal` |
| `Activo` | `bool` |

#### `CreateAccountConfigDTO` — Crear configuración

| Campo | Tipo | Validación |
|---|---|---|
| `Nombre` | `string` | `[Required]` |
| `MontoLimite` | `decimal` | `[Required]` |

#### `UpdateAccountConfigDTO` — Actualizar configuración

| Campo | Tipo | Validación |
|---|---|---|
| `IdConfig` | `int` | `[Required]` |
| `Nombre` | `string` | `[Required]` |
| `MontoLimite` | `decimal` | `[Required]` |

---

## 9. AutoMapper Profiles

### 9.1 `CurrentAccountProfile`

**Archivo:** `Features/CurrentAccount/Profile/CurrentAccountProfile.cs`

| Mapeo | Detalle |
|---|---|
| `MovimientoCc → AccountMovementDTO` | `TipoMovimiento` = nombre del tipo, `Detalle` = detalle propio o acción del tipo, `Estado` = nombre del estado, `UsuarioRegistra` = nombre + apellido, `CodigoVenta`/`FechaVenta`/`TotalVenta`/`EstadoVenta` = datos de la venta asociada, `EstadoPago` = derivado de `MontoPagado` vs `Importe` (solo para tipo 5) |
| `CreateCurrentAccountDTO → MovimientoCc` | `Importe=0`, `Fecha=DateTime.Now`, `IdEstado=2` (Aprobado), `IdTipoMovimiento=2` (ALTA_CLIENTE) |
| `TipoMovimiento → TypeMovementDTO` | `IdTipoMovimiento` = `src.IdMovimiento` |

### 9.2 `CurrentAccountConfigProfile`

**Archivo:** `Features/CurrentAccount/Profile/CurrentAccountConfigProfile.cs`

| Mapeo | Detalle |
|---|---|
| `CreateAccountConfigDTO → ConfiguracionCc` | `IdConfig` ignorado, `Activo=true` |
| `UpdateAccountConfigDTO → ConfiguracionCc` | `Activo` ignorado |
| `ConfiguracionCc → AccountConfigDTO` | Mapeo directo |

---

## 10. Códigos de Error

### 10.1 `CurrentAccountCode`

**Archivo:** `Features/CurrentAccount/Message/CurrentAccountCode.cs`

| Código | Mensaje |
|---|---|
| `account_not_found` | La cuenta indicada no existe. |
| `account_already_active` | La cuenta corriente ya está activa. |
| `unexpected_error` | Ocurrió un error inesperado, por favor intente nuevamente. |

### 10.2 `AccountConfigCode`

**Archivo:** `Features/CurrentAccount/Message/AccountConfigErrorCode.cs`

| Código | Mensaje |
|---|---|
| `account_config_not_found` | La configuración de cuenta indicada no existe. |
| `account_config_already_active` | La configuración de cuenta ya está activa. |
| `account_config_name_exists` | El nombre de la configuración de cuenta ya existe. |
| `account_config_limit_exists` | El límite de la configuración de cuenta ya existe. |
| `account_config_already_inactive` | La configuración de cuenta ya está inactiva. |
| `account_config_creation_failed` | La creación de la configuración de cuenta falló. |
| `account_config_update_failed` | La actualización de la configuración de cuenta falló. |
| `unexpected_error` | Ocurrió un error inesperado, por favor intente nuevamente. |

---

## 11. Generación de Comprobante PDF

### `PaymentReceiptDataSource`

**Archivo:** `Features/CurrentAccount/PDF/PaymentReceiptDataSource.cs`

Contiene los datos necesarios para renderizar el PDF:
- **Ferretería:** nombre, dirección, teléfono, email, CUIT.
- **Cliente:** nombre, DNI/CUIT, teléfono.
- **Pago:** tipo, detalle, importe, saldo resultante, usuario que registra.
- **Venta asociada** (solo para pago_factura): código y total.

### `PaymentReceiptDocument`

**Archivo:** `Features/CurrentAccount/PDF/PaymentReceiptDocument.cs`

Implementa `IDocument` de QuestPDF. Estructura del documento:
- **Header:** datos de la ferretería + título "COMPROBANTE DE PAGO - CUENTA CORRIENTE" + fecha.
- **Content:**
  - Sección cliente con nombre, DNI/CUIT, teléfono y usuario que registra.
  - Sección detalle del pago: tipo, descripción, venta asociada (si aplica), importe pagado (resaltado en verde), saldo resultante (rojo si deuda, verde si a favor).
- **Footer:** "Gracias por su pago" + timestamp de generación.

---

## 12. Diagrama de Flujo: Registro de un Movimiento

```
[Frontend] POST /api/CurrentAccount/register-movement
            │
            ▼
    AddMovementDTO (validación)
            │
            ▼
   CurrentAccountService.RegisterMovement()
            │
            ├── 1. Obtener último movimiento del cliente
            │       → saldo actual + límite actual
            │
            ├── 2. MovementStrategyFactory.GetStrategy(tipoMovimiento)
            │       → IMovementStrategy (Sale/Payment/Interest/DebitNote/CreditNote)
            │
            ├── 3. strategy.Calculate(saldoBase, limiteBase, importe)
            │       → CalculationResult(newBalance, newLimit)
            │
            ├── 4. Crear MovimientoCc con saldo/límite calculados
            │       → Persistir
            │
            ├── 5. Si PAGO_FACTURA → AllocarPagoFactura()
            │       Suma MontoPagado al consumo vinculado (cap al importe)
            │
            ├── 5. Si PAGO_GLOBAL → AllocarPagoGlobal()
            │       Distribuye FIFO en consumos impagos
            │
            └── 6. Retorna idMovimiento
                    → Frontend puede solicitar PDF: GET /payment-receipt/{id}
```

---

## 13. Resumen de Archivos del Módulo

```
Features/CurrentAccount/
├── Controllers/
│   ├── CurrentAccountController.cs          # 6 endpoints de movimientos
│   └── AccountConfigController.cs           # 5 endpoints de configuración
├── DTO/
│   ├── AccountConfigDTO/
│   │   ├── AccountConfigDTO.cs              # Respuesta de config
│   │   ├── CreateAccountConfigDTO.cs        # Creación de config
│   │   └── UpdateAccountConfigDTO.cs        # Actualización de config
│   └── MovementDTO/
│       ├── AccountMovementDTO.cs            # Respuesta de movimiento
│       ├── AddMovementDTO.cs                # Registrar movimiento (con validaciones)
│       ├── CreateCurrentAccount.cs          # Crear CC (con validaciones)
│       ├── PendingSalePaymentDTO.cs         # Venta pendiente de pago
│       └── TypeMovementDTO.cs               # Tipo de movimiento
├── Message/
│   ├── CurrentAccountCode.cs                # Errores de CC
│   └── AccountConfigErrorCode.cs            # Errores de configuración
├── PDF/
│   ├── PaymentReceiptDataSource.cs          # Datos para el comprobante
│   └── PaymentReceiptDocument.cs            # Generación PDF (QuestPDF)
├── Profile/
│   ├── CurrentAccountProfile.cs             # Mapeos de movimientos
│   └── CurrentAccountConfigProfile.cs       # Mapeos de configuración
├── Repository/
│   ├── AccountMovementRepository/
│   │   ├── IAccountMovementRepository.cs    # Interfaz (9 métodos)
│   │   └── AccountMovementRepository.cs     # Implementación EF Core
│   └── AccountConfigRepository/
│       ├── IAccountConfigRepository.cs      # Interfaz (9 métodos)
│       └── AccountConfigRepository.cs       # Implementación EF Core
├── Services/
│   ├── CurrentAccountService/
│   │   ├── ICurrentAccountService.cs        # Interfaz (6 métodos)
│   │   ├── CurrentAccountService.cs         # Lógica de negocio
│   │   └── StrategyCurrentAccount/
│   │       ├── IMovementStrategy.cs         # Interfaz + CalculationResult
│   │       ├── MovementStrategy.cs          # 5 estrategias concretas
│   │       ├── MovementStrategyFactory.cs   # Factory
│   │       └── TypeMovements.cs             # Enum TypeMovement
│   └── AccountConfigService/
│       ├── IAccountConfigService.cs         # Interfaz (5 métodos)
│       └── AccountConfigService.cs          # Lógica de negocio
└── AccountConfig_Implementation_Documentation.md  # Doc existente
```
