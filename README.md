# Venta Stock WebAPI

API REST para gestión de ventas e inventario, desarrollada como proyecto final universitario (Programador Universitario). Implementada con ASP.NET Core 8.0 y PostgreSQL.

## Stack tecnológico

| Componente | Tecnología |
|---|---|
| Framework | ASP.NET Core 8.0 |
| ORM | Entity Framework Core 9 + Npgsql |
| Base de datos | PostgreSQL (Railway) |
| Autenticación | JWT Bearer |
| Mapeo | AutoMapper 12 |
| PDF | QuestPDF |
| Excel | EPPlus 6 |
| Documentación | Swagger / OpenAPI |

## Arquitectura

Organización por **feature folders** con capas estrictas:

```
Features/
├── <Feature>/
│   ├── Controllers/     # Solo llamadas al servicio + HTTP status
│   ├── Services/        # Lógica de negocio
│   ├── Repository/      # Acceso a datos (interfaz + implementación)
│   ├── DTO/             # Input/output contracts
│   ├── Message/         # Enum de errores + diccionario de mensajes
│   └── Profile/         # Configuración de AutoMapper
Shared/
├── ResultPattern/       # Result<T> para manejo de errores tipado
├── Paged/               # PagedList<T> para paginación genérica
├── Auth/                # JWT, permisos, hashing
├── MessageProvider/     # Proveedor centralizado de mensajes de error
└── Identity/            # IUserContext para claims del usuario autenticado
Data/
├── VentaStockContext.cs # DbContext principal
├── Configurations/      # Fluent API por entidad
├── Interceptors/        # AuditSessionInterceptor
└── Audit/               # Sesión de auditoría
```

### Patrones clave

- **Result\<T\>**: todos los servicios retornan `Result<T>`. Los controllers verifican `result.IsSuccess` y mapean a HTTP status codes.
- **MessageProvider**: convierte error codes tipados a mensajes en español para el cliente.
- **PagedList\<T\>**: paginación genérica con metadata (`TotalPages`, `HasNextPage`, etc.).
- **Strategy Pattern**: flujo de ventas (contado vs. cuenta corriente) y movimientos de CC.
- **Repository Pattern**: interfaces abstraen el acceso a datos; `.AsNoTracking()` en lecturas.

## Configuración

### Requisitos

- .NET 8 SDK
- PostgreSQL (o acceso a instancia Railway)

### Variables de configuración

`appsettings.json`:

```json
{
  "ConnectionStrings": {
    "PostgresSQLConnection": "<connection-string-postgresql>"
  },
  "Jwt": {
    "Key": "<clave-secreta-256-bits>",
    "Issuer": "<issuer>",
    "Audience": "<audience>",
    "ExpirationHours": 8
  },
  "Defaults": {
    "IvaPorDefecto": 21.0
  }
}
```

### Comandos esenciales

```bash
# Restaurar dependencias
dotnet restore

# Compilar
dotnet build

# Ejecutar
dotnet run

# Hot reload en desarrollo
dotnet watch run

# Migraciones (usar dotnet tool local)
dotnet tool run dotnet-ef migrations add <Nombre>
dotnet tool run dotnet-ef database update
dotnet tool run dotnet-ef migrations remove
```

## Autenticación

La API usa **JWT Bearer**. Todos los endpoints (salvo `/api/login`) requieren el header:

```
Authorization: Bearer <token>
```

El sistema de autorización es **basado en permisos** (no solo roles). Cada endpoint puede requerir un permiso específico declarado con el atributo `[Authorize(Policy = "NombrePermiso")]`.

### Roles del sistema

| Rol | Descripción |
|---|---|
| Administrador Principal | Acceso total. No puede eliminarse. Gestiona usuarios y permisos. |
| Encargado de Precios | Productos, precios, stock, proveedores, listas de precios. |
| Vendedor | Ventas, clientes, consultas de stock. |

## Endpoints

### Autenticación

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/login` | Autenticar usuario. Retorna JWT. |

**Body:**
```json
{ "userName": "admin", "password": "******" }
```

---

### Usuarios — `api/user`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/user/users` | Listar usuarios (opcional: filtrar por ID) |
| GET | `/api/user/search` | Buscar usuarios con paginación y filtro de estado |
| POST | `/api/user/create` | Crear usuario |
| PUT | `/api/user/update` | Actualizar usuario |
| PUT | `/api/user/change-password` | Cambiar contraseña |
| DELETE | `/api/user/delete/{id}` | Baja lógica de usuario |
| PUT | `/api/user/activate/{id}` | Reactivar usuario dado de baja |

### Permisos — `api/permission`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/permission/permissions` | Listar permisos (opcional: filtrar por categoría) |

---

### Clientes — `api/cliente`

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/cliente/create` | Crear cliente |
| GET | `/api/cliente/client/{id}` | Obtener cliente por ID |
| GET | `/api/cliente/search` | Buscar clientes con paginación y filtro de estado |
| PUT | `/api/cliente/update` | Actualizar cliente |
| PUT | `/api/cliente/toggle-status` | Activar / desactivar cliente (baja lógica) |

Soporta personas físicas (DNI + nombre/apellido) y jurídicas (CUIT + razón social). Clientes de tipo *Cuenta Corriente* tienen límite de crédito configurable.

---

### Cuenta Corriente — `api/currentaccount`

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/currentaccount/create-account` | Abrir cuenta corriente para un cliente |
| GET | `/api/currentaccount/summary/{clientId}` | Resumen de cuenta (saldo, límite, crédito disponible) |
| GET | `/api/currentaccount/movements/{clientId}` | Movimientos paginados con filtros |
| GET | `/api/currentaccount/movement-types` | Tipos de movimiento disponibles |
| POST | `/api/currentaccount/register-movement` | Registrar pago (global, por factura o parcial) |
| POST | `/api/currentaccount/annul-payment` | Anular pago registrado |
| GET | `/api/currentaccount/payment-receipt/{idMovimiento}` | Descargar recibo de pago (PDF) |
| POST | `/api/currentaccount/register-debit-note` | Registrar nota de débito |
| GET | `/api/currentaccount/overdue-clients` | Clientes con facturas vencidas |
| POST | `/api/currentaccount/apply-interest/{clientId}` | Aplicar interés a un cliente |
| POST | `/api/currentaccount/apply-interest/bulk` | Aplicar interés a todos los clientes morosos |
| PUT | `/api/currentaccount/update-limit` | Actualizar límite de crédito |
| GET | `/api/currentaccount/limit-history/{clientId}` | Historial de cambios de límite |
| GET | `/api/currentaccount/pending-sales/{clientId}` | Facturas impagas de un cliente |
| GET | `/api/currentaccount/cliente/{id}/export/pdf` | Exportar estado de cuenta (PDF) |
| GET | `/api/currentaccount/cliente/{id}/export/excel` | Exportar estado de cuenta (Excel) |

#### Configuración de cuenta corriente — `api/accountconfig`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/accountconfig/account-configs` | Listar configuraciones de CC |
| GET | `/api/accountconfig/account-configs/{id}` | Obtener configuración por ID |
| POST | `/api/accountconfig/create-account-configs` | Crear configuración |
| PUT | `/api/accountconfig/update-account-configs` | Actualizar configuración |
| DELETE | `/api/accountconfig/toggle-state/{id}/{activo}` | Activar / desactivar configuración |

#### Motivos de nota de débito — `api/debitnotereason`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/debitnotereason/reasons` | Listar motivos |
| GET | `/api/debitnotereason/reasons/{id}` | Obtener por ID |
| POST | `/api/debitnotereason/reasons` | Crear motivo |
| PUT | `/api/debitnotereason/reasons` | Actualizar motivo |
| DELETE | `/api/debitnotereason/toggle-state/{id}/{activo}` | Activar / desactivar |

#### Configuración de intereses — `api/interestconfig`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/interestconfig/interest-configs` | Listar configuraciones |
| GET | `/api/interestconfig/interest-configs/current` | Obtener configuración activa |
| GET | `/api/interestconfig/interest-configs/{id}` | Obtener por ID |
| POST | `/api/interestconfig/interest-configs` | Crear configuración |
| PUT | `/api/interestconfig/interest-configs` | Actualizar |
| PUT | `/api/interestconfig/interest-configs/set-current/{id}` | Establecer como activa (inactiva la anterior) |

---

### Ventas — `api/sale`

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/sale/create` | Crear venta (puede generar venta directa o pendiente si excede límite de CC) |
| GET | `/api/sale` | Listar ventas con paginación y filtros |
| GET | `/api/sale/{id}` | Obtener venta con detalle |
| POST | `/api/sale/{id}/annul` | Anular venta y emitir nota de crédito |
| GET | `/api/sale/{id}/pdf` | Descargar comprobante de venta (PDF) |
| GET | `/api/sale/{id}/credit-note-pdf` | Descargar nota de crédito (PDF) |
| GET | `/api/sale/export/excel` | Exportar listado de ventas (Excel) |
| GET | `/api/sale/export/pdf` | Exportar listado de ventas (PDF) |
| GET | `/api/sale/cliente/{id}/export/excel` | Exportar historial de ventas de un cliente (Excel) |
| GET | `/api/sale/cliente/{id}/export/pdf` | Exportar historial de ventas de un cliente (PDF) |
| GET | `/api/sale/{id}/export/excel` | Exportar ticket de venta individual (Excel) |
| GET | `/api/sale/{id}/export/pdf` | Exportar ticket de venta individual (PDF) |

#### Ventas pendientes — `api/pendingsale`

Ventas que superaron el límite de crédito y esperan autorización del administrador.

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/pendingsale` | Listar ventas pendientes |
| GET | `/api/pendingsale/{id}` | Detalle de venta pendiente |
| POST | `/api/pendingsale/{id}/approve` | Aprobar y confirmar venta |
| POST | `/api/pendingsale/{id}/reject` | Rechazar venta con motivo |
| GET | `/api/pendingsale/stats` | Estadísticas de ventas pendientes |
| GET | `/api/pendingsale/{id}/pdf` | Descargar PDF de venta pendiente |

#### Motivos de nota de crédito — `api/sale/credit-note`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/sale/credit-note/reasons` | Listar motivos |
| GET | `/api/sale/credit-note/reasons/{id}` | Obtener por ID |
| POST | `/api/sale/credit-note/reasons` | Crear motivo |
| PUT | `/api/sale/credit-note/reasons` | Actualizar |
| PATCH | `/api/sale/credit-note/reasons/toggle-state/{id}/{activo}` | Activar / desactivar |

---

### Productos — `product`

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/product` | Crear producto |
| PUT | `/product/update` | Actualizar producto |
| GET | `/product` | Listar todos los productos |
| GET | `/product/with-details` | Listar productos con categoría y ubicación |
| GET | `/product/with-details-paged` | Listar productos paginados con filtros |
| GET | `/product/{id}` | Obtener producto por ID |
| DELETE | `/product/{id}` | Baja lógica de producto |
| PATCH | `/product/{id}/toggle-estado` | Activar / desactivar producto |
| GET | `/product/export/csv` | Exportar productos (CSV) |
| GET | `/product/export/excel` | Exportar productos (Excel) |
| GET | `/product/export/plantilla-csv` | Descargar plantilla CSV para importación |
| GET | `/product/export/plantilla-excel` | Descargar plantilla Excel para importación |
| POST | `/product/importar` | Importar productos desde Excel/CSV |
| GET | `/product/plantilla-precios` | Descargar plantilla para actualización masiva de precios |
| POST | `/product/actualizar-masivo/manual` | Actualización masiva de precios manual |
| POST | `/product/actualizar-masivo/excel` | Actualización masiva de precios desde Excel |

El precio de venta se calcula siempre: `Precio = Costo × (1 + PorcentajeGanancia / 100)`. Nunca se ingresa manualmente.

---

### Categorías — `category`

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/category` | Crear categoría |
| PUT | `/category/update` | Actualizar categoría |
| GET | `/category` | Listar categorías |
| GET | `/category/{id}` | Obtener categoría por ID |
| DELETE | `/category/{id}` | Eliminar categoría |

---

### Ubicaciones — `api/location`

Ubicaciones de almacén en formato `Fila-Sección-Nivel` (ej. `01 A 03`).

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/location/search` | Buscar ubicaciones con paginación |
| GET | `/api/location/{id}` | Obtener ubicación por ID |
| POST | `/api/location/create` | Crear ubicación |
| PUT | `/api/location/update/{id}` | Actualizar ubicación |
| DELETE | `/api/location/delete/{id}` | Eliminar ubicación |
| PATCH | `/api/location/toggle/{id}` | Activar / desactivar |

---

### Unidades de medida — `api/unidadmedida`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/unidadmedida` | Listar unidades activas |
| GET | `/api/unidadmedida/admin` | Listar todas (incluyendo inactivas) |
| POST | `/api/unidadmedida` | Crear unidad |
| PUT | `/api/unidadmedida/{id}` | Actualizar |
| PATCH | `/api/unidadmedida/{id}/toggle` | Activar / desactivar |

---

### Movimientos de stock — `api/stockmovement`

Libro mayor (Kardex) de stock por producto.

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/api/stockmovement/ajuste-manual` | Registrar ajuste manual de stock |
| GET | `/api/stockmovement/tipos` | Listar tipos de movimiento activos |
| GET | `/api/stockmovement/tipos/admin` | Listar todos los tipos (admin) |
| POST | `/api/stockmovement/tipos` | Crear tipo de movimiento |
| PUT | `/api/stockmovement/tipos/{id}` | Actualizar tipo |
| PATCH | `/api/stockmovement/tipos/{id}/toggle` | Activar / desactivar tipo |
| GET | `/api/stockmovement/producto/{id}/movimientos` | Historial de movimientos de un producto |

---

### Proveedores — `proveedor`

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/proveedor` | Crear proveedor |
| PUT | `/proveedor/update` | Actualizar proveedor |
| GET | `/proveedor` | Listar proveedores |
| GET | `/proveedor/search` | Buscar proveedores con paginación |
| GET | `/proveedor/{id}` | Obtener por ID |
| DELETE | `/proveedor/{id}` | Baja lógica |
| PATCH | `/proveedor/{id}/toggle-estado` | Activar / desactivar |
| GET | `/proveedor/export/excel` | Exportar proveedores (Excel) |
| GET | `/proveedor/export/pdf` | Exportar proveedores (PDF) |

---

### Compras a proveedores — `compraproveedor`

| Método | Ruta | Descripción |
|---|---|---|
| POST | `/compraproveedor` | Registrar orden de compra |
| GET | `/compraproveedor` | Listar compras con paginación y filtros |
| GET | `/compraproveedor/{id}` | Detalle de compra |
| GET | `/compraproveedor/proveedor/{id}` | Compras de un proveedor |
| POST | `/compraproveedor/{id}/anular` | Anular compra (revierte stock, no precios) |
| GET | `/compraproveedor/{id}/export/excel` | Exportar compra individual (Excel) |
| GET | `/compraproveedor/{id}/export/pdf` | Exportar compra individual (PDF) |
| GET | `/compraproveedor/proveedor/{id}/export/excel` | Exportar compras por proveedor (Excel) |
| GET | `/compraproveedor/proveedor/{id}/export/pdf` | Exportar compras por proveedor (PDF) |
| GET | `/compraproveedor/export/excel` | Exportar todas las compras (Excel) |
| GET | `/compraproveedor/export/pdf` | Exportar todas las compras (PDF) |

El costo real por ítem se calcula: `CostoReal = PrecioUnitario × (1 - Desc%) × (1 + IVA%)`.

---

### Listas de precios — `listaprecio`

Catálogo pasivo de precios por proveedor. No modifica precios de productos salvo con el flag `actualizarPrecioVenta`.

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/listaprecio/proveedor/{id}` | Listas de un proveedor |
| GET | `/listaprecio/{id}` | Detalle de lista |
| POST | `/listaprecio` | Crear lista |
| PUT | `/listaprecio` | Actualizar lista |
| DELETE | `/listaprecio/{id}` | Eliminar lista |
| PATCH | `/listaprecio/{id}/toggle-activo` | Activar / desactivar |

#### Ítems de lista — `ListaPrecio/{idLista}/items`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/ListaPrecio/{id}/items` | Listar ítems |
| POST | `/ListaPrecio/{id}/items` | Agregar ítem |
| PUT | `/ListaPrecio/{id}/items/{idProducto}` | Actualizar ítem |
| DELETE | `/ListaPrecio/{id}/items/{idProducto}` | Eliminar ítem |
| GET | `/ListaPrecio/{id}/items/plantilla-excel` | Descargar plantilla Excel |
| POST | `/ListaPrecio/{id}/items/bulk` | Alta masiva de ítems |
| POST | `/ListaPrecio/{id}/items/import` | Importar ítems desde Excel |

---

### Reportes — `api/report`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/report/total-vendido` | Total vendido en un rango de fechas |
| GET | `/api/report/ventas-por-periodo` | Ventas agrupadas por día / mes / año |
| GET | `/api/report/articulo-mas-vendido` | Artículo más vendido |
| GET | `/api/report/productos-mas-vendidos` | Top N productos más vendidos |
| GET | `/api/report/categorias-mas-vendidas` | Categorías más vendidas |
| GET | `/api/report/margen-utilidad` | Análisis de margen de utilidad |
| GET | `/api/report/clientes-frecuentes` | Clientes más frecuentes |
| GET | `/api/report/tiempo-promedio-cobro` | Tiempo promedio de cobro |
| GET | `/api/report/deuda-total` | Deuda total de clientes |
| GET | `/api/report/clientes-saldo-deudor` | Clientes con saldo deudor |

---

### Auditoría — `api/audit`

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/audit/search` | Buscar registros de auditoría con filtros (acción, entidad, usuario, rango de fechas) |

---

### Ferretería — `api/ferreteria`

Datos del negocio usados en comprobantes (nombre, dirección, CUIT, etc.).

| Método | Ruta | Descripción |
|---|---|---|
| GET | `/api/ferreteria` | Obtener datos del negocio |
| PUT | `/api/ferreteria/update` | Actualizar datos del negocio |

---

## Reglas de negocio principales

| Área | Regla |
|---|---|
| **Precio** | `Precio = Costo × (1 + PorcentajeGanancia / 100)` — siempre calculado, nunca ingresado |
| **Crédito disponible** | `CréditoDisponible = LímiteCuenta + Max(0, -SaldoActual)` |
| **Costo en compras** | `CostoReal = PrecioUnitario × (1 - Desc%) × (1 + IVA%)` |
| **Venta con CC** | Si excede el límite, queda en estado Pendiente hasta aprobación del administrador |
| **Anulación de compra** | Revierte stock, no precios ni costos (el usuario los corrige manualmente) |
| **Lista de precios** | Catálogo pasivo. Solo actualiza precios de productos si `actualizarPrecioVenta = true` |
| **Códigos de barra** | Unicidad global — un barcode no puede existir en dos productos distintos |
| **Interés activo** | Solo una configuración de intereses activa a la vez |
| **Baja de registros** | Siempre lógica (campo `FechaBaja`), nunca física |

## Estructura de la base de datos

Entidades principales y sus relaciones:

```
Usuario ──── PermisoUsuario ──── Permiso ──── CategoriaPermiso
Cliente ──── ConfiguracionCc ──── MovimientoCc ──── TipoMovimiento
Producto ──── CodigoBarra
         ──── Categorium
         ──── Ubicacion
         ──── UnidadMedida
         ──── MovimientoStock ──── TipoMovimientoStock
Ventum ────── DetalleVentum ──── Producto
         ──── VentaPendiente
CompraProveedor ── CompraProveedorDetalle ── Producto
Proveedor ──── ListaPrecio ──── ProductoListaprecioProveedor
Auditoria
Ferreteria
```

## CORS

Configurado para el frontend en `http://localhost:5173`. Para otros orígenes, modificar la policy `Frontend` en `Program.cs`.

## Herramientas de desarrollo

- **Swagger UI**: disponible en `/swagger` cuando `ASPNETCORE_ENVIRONMENT=Development`
- **Migraciones**: usar el tool local (`dotnet-tools.json`) para evitar conflictos de versión con el SDK global

```bash
# Listar migraciones aplicadas
dotnet tool run dotnet-ef migrations list

# Revertir última migración
dotnet tool run dotnet-ef migrations remove
```