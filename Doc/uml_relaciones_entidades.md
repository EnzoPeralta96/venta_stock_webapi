# Relaciones entre Entidades — Diagrama UML de Dominio

## Referencias de notación

| Símbolo | Tipo | Cuándo usarlo |
|---|---|---|
| ◆——< | Composición | La parte no puede existir sin el todo |
| ◇——< | Agregación | La parte puede existir sin el todo. El diamante va en el TODO (el que tiene la lista) |
| ——> | Asociación | Una clase referencia a otra sin contenencia |

---

## Módulo: Usuarios y Permisos

| Entidad | Relación | Entidad | Tipo | Razón |
|---|---|---|---|---|
| `CategoriaPermiso` | ◇——< | `Permiso` | Agregación | CategoriaPermiso tiene lista de Permisos, Permiso existe sin ella |
| `Usuario` | ◆——< | `PermisoUsuario` | Composición | PermisoUsuario no existe sin Usuario |
| `Permiso` | ◆——< | `PermisoUsuario` | Composición | PermisoUsuario no existe sin Permiso |

---

## Módulo: Productos

| Entidad | Relación | Entidad | Tipo | Razón |
|---|---|---|---|---|
| `Categoria` | ◇——< | `Producto` | Agregación | Categoria tiene lista de Productos, existe sin ellos |
| `Ubicacion` | ◇——< | `Producto` | Agregación | Ubicacion tiene lista de Productos, existe sin ellos |
| `UnidadMedida` | ◇——< | `Producto` | Agregación | UnidadMedida tiene lista de Productos, existe sin ellos |
| `Producto` | ◆——< | `CodigoBarra` | Composición | CodigoBarra no existe sin Producto |
| `Producto` | ◆——< | `MovimientoStock` | Composición | MovimientoStock no existe sin Producto |
| `TipoMovimientoStock` | ◇——< | `MovimientoStock` | Agregación | TipoMovimientoStock tiene lista de movimientos, existe sin ellos |
| `MovimientoStock` | ——> | `Usuario` | Asociación | Referencia de trazabilidad, IdUsuario nullable |

---

## Módulo: Clientes

| Entidad | Relación | Entidad | Tipo | Razón |
|---|---|---|---|---|
| `Cliente` | ◆——< | `Venta` | Composición | Venta sin Cliente no tiene sentido de negocio |
| `Cliente` | ◆——< | `VentaPendiente` | Composición | VentaPendiente sin Cliente no existe |
| `Cliente` | ◆——< | `MovimientoCc` | Composición | Movimiento CC pertenece integralmente al Cliente |

---

## Módulo: Ventas

| Entidad | Relación | Entidad | Tipo | Razón |
|---|---|---|---|---|
| `Venta` | ◆——< | `DetalleVenta` | Composición | DetalleVenta no existe sin Venta |
| `VentaPendiente` | ◆——< | `DetalleVentaPendiente` | Composición | DetalleVentaPendiente no existe sin VentaPendiente |
| `MedioPago` | ◇——< | `Venta` | Agregación | MedioPago tiene lista de Ventas, existe sin ellas |
| `Estado` | ◇——< | `Venta` | Agregación | Estado tiene lista de Ventas, existe sin ellas |
| `Usuario` | ◇——< | `Venta` | Agregación | Usuario tiene lista de Ventas que registró, existe sin ellas |
| `MedioPago` | ◇——< | `VentaPendiente` | Agregación | Igual que con Venta |
| `Estado` | ◇——< | `VentaPendiente` | Agregación | Igual que con Venta |
| `DetalleVenta` | ——> | `Producto` | Asociación | Referencia al producto vendido |
| `DetalleVentaPendiente` | ——> | `Producto` | Asociación | Referencia al producto |
| `VentaPendiente` | ——> | `Venta` | Asociación | Referencia a la venta generada al aprobar |

---

## Módulo: Proveedores y Compras

| Entidad | Relación | Entidad | Tipo | Razón |
|---|---|---|---|---|
| `Proveedor` | ◆——< | `CompraProveedor` | Composición | CompraProveedor sin Proveedor no existe (IdProveedor NOT NULL) |
| `CompraProveedor` | ◆——< | `CompraProveedorDetalle` | Composición | Detalle no existe sin la Compra |
| `Proveedor` | ◇——< | `ListaPrecio` | Agregación | ListaPrecio puede existir sin Proveedor (IdProveedor nullable) |
| `ListaPrecio` | ◆——< | `ItemListaPrecio` | Composición | Item no existe sin la Lista |
| `CompraProveedorDetalle` | ——> | `Producto` | Asociación | Referencia al producto comprado |
| `ItemListaPrecio` | ——> | `Producto` | Asociación | Referencia al producto de la lista |
| `Usuario` | ◇——< | `CompraProveedor` | Agregación | Usuario registra compras, existe sin ellas |
| `Usuario` | ◇——< | `ListaPrecio` | Agregación | Usuario registra listas, existe sin ellas |

---

## Módulo: Cuenta Corriente

| Entidad | Relación | Entidad | Tipo | Razón |
|---|---|---|---|---|
| `TipoMovimiento` | ◇——< | `MovimientoCc` | Agregación | TipoMovimiento tiene lista de movimientos, existe sin ellos |
| `MovimientoCc` | ——> | `Venta` | Asociación | Referencia opcional a la venta origen (IdVenta nullable) |
| `Usuario` | ◇——< | `MovimientoCc` | Agregación | Usuario registra/autoriza movimientos, existe sin ellos |
