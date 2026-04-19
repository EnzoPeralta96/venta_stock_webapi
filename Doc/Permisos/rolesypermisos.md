# Roles y Permisos del Sistema

**Sistema**: SVS - Sistema de Venta-Stock (Ferretería)
**Última actualización**: 2026-04-19
**Versión**: 1.0

---

## Descripción General

El sistema utiliza un esquema de **autorización basada en permisos granulares** organizados por categorías. Cada usuario tiene un rol que determina el conjunto de permisos sugerido por defecto, pero el administrador puede ajustar los permisos individualmente.

Los permisos se almacenan en la tabla `permiso` y se asocian a usuarios mediante la tabla `permiso_usuario` (relación muchos a muchos).

La autenticación se realiza mediante **JWT**. El token incluye el ID, nombre de usuario y rol. Los permisos se validan en cada endpoint del backend mediante políticas de autorización del tipo `PERM:{CODIGO}`.

---

## Roles del Sistema

El sistema define tres roles predefinidos. Al seleccionar un rol en el formulario de usuario, se auto-seleccionan los permisos correspondientes. El administrador puede modificarlos manualmente después.

| Rol | Descripción |
|-----|-------------|
| `Administracion` | Acceso total al sistema. Incluye todos los permisos disponibles. |
| `Ventas` | Gestión de ventas, clientes y cobros de cuenta corriente. |
| `Control de stock y precios` | Gestión de productos, stock, proveedores, listas de precios y compras. |

---

## Catálogo Completo de Permisos

### Categoría 1 — Gestión de Usuarios

| ID | Código | Descripción |
|----|--------|-------------|
| 1 | `USR_CREATE` | Crear usuarios |
| 2 | `USR_READ` | Ver usuarios |
| 3 | `USR_UPDATE` | Modificar usuarios |
| 4 | `USR_DELETE` | Eliminar usuarios |
| 5 | `USR_PASSWORD_UPDATE` | Modificar contraseña de un usuario |
| 6 | `USR_PERM_READ` | Ver lista de permisos disponibles |

### Categoría 2 — Reportes y Auditoría

| ID | Código | Descripción |
|----|--------|-------------|
| 7 | `REP_GENERATE` | Generar reportes |
| 8 | `REP_EXPORT` | Exportar reportes |
| 9 | `HIS_VIEW` | Ver historial de acciones |

### Categoría 3 — Ventas y Finanzas

| ID | Código | Descripción |
|----|--------|-------------|
| 10 | `VEN_CREATE` | Registrar venta |
| 11 | `VEN_READ` | Leer todas las ventas |
| 12 | `VEN_INVOICE` | Emitir factura |
| 13 | `VEN_NO_STOCK` | Vender sin stock |
| 14 | `VEN_AUTH_OVERLIMIT` | Autorizar venta con límite de crédito superado (inline, sin dejar pendiente) |
| 15 | `CC_VIEW` | Ver movimientos de cuenta corriente |
| 16 | `CC_MANAGE` | Crear/actualizar/eliminar configuraciones de CC |
| 17 | `CC_REGISTER_PAYMENT` | Registrar pagos y movimientos en CC |
| 18 | `CC_NOTE_DEBIT` | Generar nota de débito |
| 19 | `CC_NOTE_CREDIT` | Generar nota de crédito |
| 20 | `SALE_AUTHORIZE` | Autorizar o rechazar ventas pendientes que exceden el límite de crédito |
| 21 | `PROV_CREATE` | Crear proveedores |
| 22 | `PROV_READ` | Ver proveedores |
| 23 | `PROV_UPDATE` | Editar proveedores |
| 24 | `PROV_DELETE` | Eliminar proveedores |

### Categoría 4 — Productos y Stock

| ID | Código | Descripción |
|----|--------|-------------|
| 25 | `PROD_CREATE` | Crear productos |
| 26 | `PROD_READ` | Ver productos |
| 27 | `PROD_UPDATE` | Modificar productos |
| 28 | `PROD_DELETE` | Eliminar productos |
| 29 | `PROD_BARCODE` | Cargar códigos de barra |
| 30 | `PROD_PRICE_UPDATE` | Actualizar precios |
| 31 | `PROD_STOCK_LOW` | Ver stock bajo |
| 32 | `PROD_STOCK_IN` | Registrar ingreso de mercadería |
| 33 | `LP_CREATE` | Crear lista de precios de proveedor |
| 34 | `LP_READ` | Ver listas de precios de proveedor |
| 35 | `LP_UPDATE` | Modificar lista de precios de proveedor |
| 36 | `LP_DELETE` | Eliminar lista de precios de proveedor |
| 37 | `LP_TOGGLE` | Activar/desactivar lista de precios |
| 38 | `LP_ITEM_ADD` | Agregar producto a lista de precios |
| 39 | `LP_ITEM_UPDATE` | Modificar precio en lista de precios |
| 40 | `LP_ITEM_DELETE` | Quitar producto de lista de precios |
| 41 | `COMP_CREATE` | Registrar compra a proveedor |
| 42 | `COMP_READ` | Ver compras a proveedor |
| 43 | `COMP_UPDATE` | Modificar compra a proveedor |
| 44 | `COMP_DELETE` | Eliminar compra a proveedor |

### Categoría 5 — Clientes

| ID | Código | Descripción |
|----|--------|-------------|
| 45 | `CLI_CREATE` | Crear clientes |
| 46 | `CLI_READ` | Ver clientes |
| 47 | `CLI_UPDATE` | Modificar clientes |
| 48 | `CLI_DELETE` | Eliminar clientes |

### Categoría 6 — Búsquedas

| ID | Código | Descripción |
|----|--------|-------------|
| 49 | `SEARCH_USER` | Buscar usuarios |
| 50 | `SEARCH_CLIENT` | Buscar clientes |
| 51 | `SEARCH_PRODUCT` | Buscar productos |
| 52 | `SEARCH_SALE` | Buscar ventas |

### Categoría 7 — Administración del Sistema

| ID | Código | Descripción |
|----|--------|-------------|
| 53 | `SYS_CONFIG` | Configurar datos del sistema (ferretería) |

---

## Permisos por Rol

### Rol: Administracion

Tiene **todos los permisos del sistema** (54 en total). Es el único rol que puede:
- Crear, modificar y eliminar usuarios
- Asignar y cambiar roles
- Autorizar ventas que exceden el límite de crédito
- Acceder al historial de auditoría
- Configurar parámetros del sistema

> El administrador principal predefinido no puede ser eliminado por ningún otro usuario.

---

### Rol: Ventas

Orientado al **vendedor** que realiza operaciones diarias de venta y atención al cliente.

| Categoría | Códigos |
|-----------|---------|
| Ventas y Finanzas | `VEN_CREATE`, `VEN_READ`, `VEN_INVOICE`, `VEN_NO_STOCK`, `CC_VIEW`, `CC_REGISTER_PAYMENT` |
| Productos y Stock | `PROD_READ` |
| Clientes | `CLI_CREATE`, `CLI_READ`, `CLI_UPDATE` |
| Búsquedas | `SEARCH_SALE`, `SEARCH_PRODUCT`, `SEARCH_CLIENT` |

**Total**: 13 permisos

**Notas importantes**:
- No tiene `VEN_AUTH_OVERLIMIT`: si una venta supera el límite de crédito, queda en estado **Pendiente** para aprobación del administrador.
- No tiene `SALE_AUTHORIZE`: no puede aprobar ni rechazar ventas pendientes.
- No tiene `CLI_DELETE`: no puede eliminar clientes.
- Puede ver ventas pendientes (`VEN_READ`) pero no tiene los botones de aprobar/rechazar en la UI.

---

### Rol: Control de stock y precios

Orientado al **encargado de stock y precios** que gestiona el inventario, los precios y las relaciones con proveedores.

| Categoría | Códigos |
|-----------|---------|
| Productos y Stock | `PROD_CREATE`, `PROD_READ`, `PROD_UPDATE`, `PROD_DELETE`, `PROD_BARCODE`, `PROD_PRICE_UPDATE`, `PROD_STOCK_LOW`, `PROD_STOCK_IN` |
| Proveedores | `PROV_CREATE`, `PROV_READ`, `PROV_UPDATE`, `PROV_DELETE` |
| Listas de precios | `LP_CREATE`, `LP_READ`, `LP_UPDATE`, `LP_DELETE`, `LP_TOGGLE`, `LP_ITEM_ADD`, `LP_ITEM_UPDATE`, `LP_ITEM_DELETE` |
| Compras | `COMP_CREATE`, `COMP_READ`, `COMP_UPDATE`, `COMP_DELETE` |
| Reportes | `REP_GENERATE`, `REP_EXPORT` |
| Búsquedas | `SEARCH_PRODUCT` |

**Total**: 30 permisos

**Notas importantes**:
- Control total sobre productos, proveedores, listas de precios y compras.
- Puede generar y exportar reportes (útil para decisiones de reposición y precios).
- No tiene acceso a ventas, clientes ni cuenta corriente.
- No tiene acceso a gestión de usuarios ni configuración del sistema.

---

## Flujo de Ventas con Límite de Crédito Superado

Este flujo involucra dos permisos distintos que suelen confundirse:

```
Vendedor intenta vender a cliente con CC
        │
        ▼
¿Supera el límite de crédito?
        │
   SÍ ─┤
        │
        ├─ ¿Tiene VEN_AUTH_OVERLIMIT?
        │        │
        │      SÍ │──► Venta se completa directamente (sin quedar pendiente)
        │        │
        │      NO │──► Venta queda en estado PENDIENTE
        │                  │
        │                  ▼
        │        Admin con SALE_AUTHORIZE revisa
        │                  │
        │         ┌────────┴────────┐
        │       Aprueba          Rechaza
        │         │                │
        │    Venta procesada   Venta cancelada
        │
   NO ──┴──► Venta se completa normalmente
```

| Permiso | Rol típico | Efecto |
|---------|-----------|--------|
| `VEN_AUTH_OVERLIMIT` | Vendedor senior | Completa la venta directamente sin pasar por pendientes |
| `SALE_AUTHORIZE` | Administrador | Accede al panel de ventas pendientes y puede aprobar o rechazar |

---

## Implementación Técnica

### Backend (ASP.NET Core)

Los permisos se validan mediante políticas de autorización registradas en `Program.cs`:

```csharp
// Ejemplo de uso en controlador
[HttpGet]
[Authorize(Policy = "PERM:VEN_READ")]
public async Task<IActionResult> GetPendingSales() { ... }

[HttpPost("{id:int}/approve")]
[Authorize(Policy = "PERM:SALE_AUTHORIZE")]
public async Task<IActionResult> ApproveSale(...) { ... }
```

### Frontend (React)

La auto-selección de permisos por rol se implementa en `UserFormDrawer.jsx` mediante la constante `ROLE_PERMISSIONS`. Al elegir un rol en el formulario, se marcan automáticamente los checkboxes correspondientes. El administrador puede ajustarlos manualmente.

El control de visibilidad en la UI se realiza mediante el componente `<PermissionGuard permission="CODIGO">`, que oculta el contenido si el usuario no tiene el permiso requerido.

```jsx
// Ejemplo: solo admins con SALE_AUTHORIZE ven los botones de aprobar/rechazar
<PermissionGuard permission="SALE_AUTHORIZE">
  <Button onClick={handleApprove}>Aprobar</Button>
  <Button onClick={handleReject}>Rechazar</Button>
</PermissionGuard>
```

---

## Notas de Diseño

- Las eliminaciones de usuarios, clientes y productos son **lógicas** (soft delete). Nunca se eliminan registros físicamente.
- Los cambios de rol y permisos quedan registrados en el historial de auditoría con usuario responsable, fecha y hora.
- El conjunto de permisos de cada rol es una **sugerencia por defecto**. El administrador siempre puede personalizar los permisos de un usuario individual.
- Si se agregan nuevos permisos al sistema, deben actualizarse tanto el CSV de seed (`permiso.csv`) como el array `ROLE_PERMISSIONS` del frontend.
