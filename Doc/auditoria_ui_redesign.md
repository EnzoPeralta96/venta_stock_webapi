# Rediseño UI — Módulo de Auditoría / Historial de Acciones

**Fecha**: Abril 2026
**Área**: Frontend — React
**Archivos creados / modificados**:
- `frontend/src/components/Audit/auditHelpers.js` *(nuevo)*
- `frontend/src/components/Audit/AuditTable.jsx` *(reescrito)*
- `frontend/src/components/Audit/AuditTimeline.jsx` *(nuevo)*
- `frontend/src/components/Audit/AuditChangesModal.jsx` *(reescrito)*
- `frontend/src/components/Audit/AuditPage.jsx` *(modificado)*

---

## Motivación

La pantalla de auditoría anterior mostraba una tabla básica con columnas de "Valores Anteriores" y "Valores Nuevos" en texto crudo. No era legible ni descriptiva. Se rediseñó completamente para ofrecer:

- Lenguaje natural en lugar de datos técnicos
- Identificación visual rápida por tipo de acción y entidad
- Dos modos de visualización: tabla y línea de tiempo
- Modal de detalle con vistas inteligentes según el tipo de registro

---

## Archivos creados / modificados

### `auditHelpers.js`

Módulo de utilidades puras para transformar datos de auditoría en información legible.

**Funciones exportadas**:

| Función | Descripción |
|--------|-------------|
| `formatDateTime(iso)` | Formatea fecha ISO a `dd/mm/yyyy HH:MM` |
| `formatRelativeTime(iso)` | Tiempo relativo: "Hace 5 min", "Hace 2 h", "Hace 3 d" |
| `getActionConfig(accion)` | Retorna `{ label, verb, color }` para los 21 tipos de acción |
| `getActionColors(accion)` | Retorna clases Tailwind pastel `{ bg, text, border, dot }` por acción |
| `getEntityConfig(entidadTipo)` | Retorna `{ label, labelPlain }` para cada tipo de entidad |
| `buildActivityText(item)` | Arma la oración natural: `{ user, verb, entity, id }` |
| `getUserInitials(name)` | Extrae iniciales del nombre completo |
| `getUserAvatarColor(name)` | Color determinístico por nombre (hash) — consistente entre sesiones |
| `parseJSON(str)` | Parsea JSON con manejo de errores silencioso |
| `formatFieldValue(value)` | Formatea valores para mostrar (booleanos, null, fechas) |
| `humanizeFieldName(key)` | Convierte claves técnicas a etiquetas legibles (ej: `FechaBaja` → "Fecha de baja") |

**Tipos de acción cubiertos** (21 en total):

`CREATE`, `UPDATE`, `DELETE`, `LOGIN`, `LOGOUT`, `VENTA_CREADA`, `VENTA_ANULADA`, `COMPRA_CREADA`, `COMPRA_ANULADA`, `STOCK_INGRESO`, `STOCK_EGRESO`, `CC_CREADA`, `CC_PAGO`, `CC_ND`, `CC_NC`, `PRECIO_ACTUALIZADO`, `PERMISO_ASIGNADO`, `PERMISO_REMOVIDO`, `ROL_CAMBIADO`, `UPDATE_ACCIONES`, `REPORTE_GENERADO`

---

### `AuditTable.jsx` (reescrito)

Tabla principal de auditoría con diseño SaaS moderno.

**Componentes internos**:
- `UserAvatar`: círculo con iniciales del usuario en color determinístico
- `ActionBadge`: badge con color pastel semántico según tipo de acción

**Columnas**:

| Columna | Contenido |
|---------|-----------|
| Actividad | Avatar + oración natural ("Juan creó un Producto #42") + badge de acción secundario |
| Entidad | Ícono de entidad (Lucide) + nombre humanizado |
| Acción | Badge principal con color semántico pastel |
| Fecha | Tiempo relativo con tooltip de fecha completa al hover |
| Ver | Botón fantasma visible al hover de fila — abre el modal de detalle |

**Íconos por entidad**:
- `PRODUCTO` → `Package`
- `USUARIO` → `Users`
- `VENTA` → `ShoppingCart`
- `CLIENTE` → `User`
- `PROVEEDOR` → `Truck`
- `COMPRA` → `ShoppingBag`

---

### `AuditTimeline.jsx` (nuevo)

Vista alternativa en línea de tiempo vertical.

**Características**:
- Línea vertical conectora entre eventos
- Punto de color por tipo de acción con ícono de entidad dentro
- Tarjeta por entrada: avatar, oración natural, badge, tiempo relativo
- Estado de carga con skeleton animado
- Estado vacío con mensaje descriptivo

---

### `AuditChangesModal.jsx` (reescrito)

Modal de detalle que selecciona automáticamente la vista correcta según el tipo de registro.

#### Lógica de selección de vista (`resolveBodyView`)

8 reglas en cascada:

| Prioridad | Condición | Vista |
|-----------|-----------|-------|
| 1 | Entidad `VENTA` o acción `VENTA_*` | `VentaDetail` |
| 2 | Entidad `COMPRA` o acción `COMPRA_*` | `CompraDetail` |
| 3 | Acción `STOCK_INGRESO` / `STOCK_EGRESO` | `StockMovDetail` |
| 4 | Acción `CC_CREADA` | `CCDetail` |
| 5 | `UPDATE_ACCIONES` con antes y después | `BeforeAfterDetail` |
| 6 | Solo `after` (creación) | `SingleColumnDetail` modo `"created"` |
| 7 | Solo `before` (eliminación) | `SingleColumnDetail` modo `"deleted"` |
| 8 | Fallback | `BeforeAfterDetail` |

#### Vistas de detalle

**`VentaDetail`** — campos usados (PascalCase, serialización C#):
- `data.Codigo`, `data.Cliente`, `data.Total`, `data.MedioPago`, `data.Estado`

**`CompraDetail`** — campos usados:
- `data.Comprobante`, `data.Proveedor`, `data.Subtotal`, `data.Descuento`, `data.IVA`, `data.Total`, `data.CantidadItems`, `data.Motivo`

**`StockMovDetail`** — ingreso/egreso de stock

**`CCDetail`** — creación de cuenta corriente

**`BeforeAfterDetail`** — tabla comparativa "Antes / Después" para modificaciones

**`SingleColumnDetail`** — columna única para creaciones (`mode="created"`) o eliminaciones (`mode="deleted"`)

#### Estrategia de merge para anulaciones

Para `VENTA_ANULADA` y `COMPRA_ANULADA`, el campo `before` contiene los datos completos y `after` solo contiene el cambio de estado (ej: `{ Estado: "Anulada" }`). Se usa merge:

```js
const merged = { ...(before || {}), ...(after || {}) };
```

Así el modal muestra todos los datos de la entidad junto con el estado final.

#### Nota sobre serialización PascalCase

El backend serializa los objetos anónimos de auditoría con **System.Text.Json** sin configuración de camelCase, por lo que los campos en el JSON almacenado en BD son **PascalCase** (`Codigo`, `Proveedor`, `Total`). El frontend accede a ellos con esa misma casing.

---

### `AuditPage.jsx` (modificado)

- Agregado estado `viewMode` (`"table"` | `"timeline"`)
- Botones de toggle en el header con íconos `LayoutList` y `GitBranch` (Lucide)
- Renderiza `AuditTable` o `AuditTimeline` según el modo seleccionado

---

## Flujo general

```
AuditPage
  ├── toggle viewMode
  ├── AuditTable (modo tabla)
  │     ├── UserAvatar
  │     ├── ActionBadge
  │     └── botón "Ver" → AuditChangesModal
  └── AuditTimeline (modo línea de tiempo)
        └── tarjeta → AuditChangesModal

AuditChangesModal
  └── resolveBodyView(item, before, after)
        ├── VentaDetail
        ├── CompraDetail
        ├── StockMovDetail
        ├── CCDetail
        ├── BeforeAfterDetail
        └── SingleColumnDetail
```

---

## Bugs corregidos durante el desarrollo

### PascalCase vs camelCase en campos de auditoría

**Problema**: El frontend accedía a `data.codigo`, `data.proveedor`, `data.total` (camelCase) pero el backend serializa con PascalCase (`data.Codigo`, `data.Proveedor`, `data.Total`).

**Causa**: C# serializa tipos anónimos con `System.Text.Json` usando el nombre exacto de las propiedades definidas, que en C# son PascalCase por convención.

**Fix**: Actualizar todos los accesos en `VentaDetail` y `CompraDetail` a PascalCase.

---

### Anulaciones mostrando datos incompletos

**Problema**: `VENTA_ANULADA` y `COMPRA_ANULADA` mostraban campos vacíos (`—`) porque el código hacía `data = after ?? before`, y `after` solo contenía `{ Estado: "Anulada" }`.

**Fix**: Merge de ambos objetos: `const merged = { ...(before || {}), ...(after || {}) }`.
