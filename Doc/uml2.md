@startuml Sistema_VentaStock                                                                  
                                                                                                
  skinparam classAttributeIconSize 0                                                            
  skinparam packageStyle rectangle                                                              
  skinparam linetype ortho                                                                      

  ' =============================================
  ' CLIENTES
  ' =============================================
  package "Clientes" #d5f5e3 {
    class Cliente {
      +IdCliente : int
      +Nombre : string?
      +Apellido : string?
      +RazonSocial : string?
      +Cuit : string?
      +Dni : string?
      +Telefono : string?
      +Mail : string?
      +FechaAlta : DateOnly?
      +FechaBaja : DateOnly?
      --
      +Crear(dto) : ClientResponseDTO
      +ObtenerPorId(id) : ClientResponseDTO
      +BuscarPaginado(pageIndex, pageSize, searchTerm, estado) : PagedList<ClientResponseDTO>
      +Actualizar(dto) : ClientResponseDTO
      +CambiarEstado(dto) : string
    }
  }

  ' =============================================
  ' CUENTA CORRIENTE
  ' =============================================
  package "Cuenta Corriente" #fef9e7 {
    class MovimientoCc {
      +IdMovimiento : int
      +Importe : decimal?
      +Fecha : DateTime?
      +Detalle : string?
      +SaldoActual : decimal?
      +LimiteCuenta : decimal?
      +IdTipoMovimiento : int?
      +IdCliente : int?
      +IdUsuarioRegistra : int?
      +IdUsuarioAutoriza : int?
      +IdVenta : int?
      +MontoPagado : decimal?
      +EsAnulado : bool
      +IdMotivoNd : int?
      +IdMotivoNc : int?
      --
      +ObtenerResumen(clientId) : AccountSummaryDTO
      +ObtenerMovimientosPaginado(clientId, pageIndex, searchTerm, fechaDesde, fechaHasta,
  idTipo) : PagedList<AccountMovementDTO>
      +ObtenerHistorialLimite(clientId) : List<AccountMovementDTO>
      +ActualizarLimite(dto) : bool
      +ExportarPdf(clientId) : byte[]
      +ExportarExcel(clientId) : byte[]
    }

    class TipoMovimiento {
      +IdMovimiento : int
      +Nombre : string?
      +Accion : string?
    }

    class MotivoNotaDebito {
      +IdMotivo : int
      +Nombre : string
      +Activo : bool
      +Categoria : string
    }
  }

  ' =============================================
  ' VENTAS
  ' =============================================
  package "Ventas" #fadbd8 {
    class Venta {
      +IdVenta : int
      +CodigoVenta : string
      +Fecha : DateTime?
      +Total : decimal?
      +IdMedioPago : int?
      +IdCliente : int?
      +IdUsuario : int?
      +IdEstado : int?
      +IdMotivoNc : int?
      +DetalleNc : string?
      --
      +Crear(dto) : VentaResponseDTO
      +ObtenerPaginado(pageIndex, searchTerm, estado, idCliente) : PagedList<VentaDTO>
      +ObtenerPorId(id) : VentaDTO
      +Anular(id, dto) : bool
      +ExportarComprobantesPdf(fechaDesde, fechaHasta) : byte[]
      +ExportarComprobantesExcel(fechaDesde, fechaHasta) : byte[]
      +ExportarNotaCreditoPdf(id) : byte[]
    }

    class VentaPendiente {
      +IdVentaPendiente : int
      +CodigoVenta : string
      +IdCliente : int
      +IdUsuarioVendedor : int
      +IdMedioPago : int
      +Total : decimal
      +SaldoActual : decimal?
      +LimiteCuenta : decimal?
      +SaldoDespuesVenta : decimal?
      +Excedente : decimal?
      +IdEstado : int
      +IdUsuarioAutoriza : int?
      +FechaAutorizacion : DateTime?
      +ObservacionesAutorizacion : string
      +FechaRegistro : DateTime
      +IdVentaGenerada : int?
      --
      +ObtenerPaginado(pageIndex, pageSize, searchTerm) : PagedList<VentaPendienteDTO>
      +Aprobar(id, dto) : bool
      +Rechazar(id, dto) : bool
    }

    class DetalleVenta {
      +IdVenta : int
      +IdProducto : int
      +Cantidad : decimal?
      +PrecioVenta : decimal?
      +SubTotal : decimal?
    }

    class DetalleVentaPendiente {
      +IdDetalle : int
      +IdVentaPendiente : int
      +IdProducto : int
      +Cantidad : decimal
      +PrecioVenta : decimal
      +Subtotal : decimal
    }

    class MedioPago {
      +IdMedioPago : int
      +MedioPago1 : string?
    }

    class Estado {
      +IdEstado : int
      +Estado1 : string?
    }

    class MotivoNotaCredito {
      +IdMotivo : int
      +Nombre : string
      +Activo : bool
    }
  }

  ' =============================================
  ' PROVEEDORES Y COMPRAS
  ' =============================================
  package "Proveedores y Compras" #e8daef {
    class Proveedor {
      +IdProveedor : int
      +Proveedor1 : string
      +Direccion : string?
      +Telefono : string?
      +Activo : bool
      +FechaBaja : DateTime?
      --
      +Crear(dto) : bool
      +Actualizar(dto) : bool
      +ObtenerTodos() : List<ProveedorDTO>
      +ObtenerPorId(id) : ProveedorDTO
      +Eliminar(id) : bool
      +CambiarEstado(id) : bool
      +ObtenerPaginado(pageIndex, pageSize, searchTerm, estado) : PagedList<ProveedorDTO>
      +ExportarExcel() : byte[]
      +ExportarPdf() : byte[]
    }

    class CompraProveedor {
      +IdCompraProveedor : int
      +IdProveedor : int
      +Fecha : DateOnly
      +FechaVencimiento : DateOnly?
      +TipoComprobante : string?
      +NumeroComprobante : string?
      +Observacion : string?
      +Subtotal : decimal
      +DescuentoTotal : decimal
      +IvaTotal : decimal
      +Total : decimal
      +IdUsuario : int?
      +Activo : bool
      --
      +Crear(dto) : CompraProveedorResponseDTO
      +ObtenerPaginado(pageIndex, pageSize, search, activo, fechaDesde, fechaHasta) :
  PagedList<CompraDTO>
      +ObtenerPorProveedor(idProveedor, pageIndex, pageSize, activo, fechaDesde, fechaHasta) :
  PagedList<CompraDTO>
      +ObtenerPorId(id) : CompraDTO
      +Anular(id, dto) : bool
      +ExportarListadoExcel(fechaDesde, fechaHasta) : byte[]
      +ExportarListadoPdf(fechaDesde, fechaHasta) : byte[]
      +ExportarCompraExcel(id) : byte[]
      +ExportarCompraPdf(id) : byte[]
    }

    class CompraProveedorDetalle {
      +IdCompraProveedorDetalle : int
      +IdCompraProveedor : int
      +IdProducto : int
      +Cantidad : decimal
      +PrecioUnitario : decimal
      +DescuentoPorcentaje : decimal
      +IvaPorcentaje : decimal
      +Subtotal : decimal
      +Total : decimal
    }

    class ListaPrecio {
      +IdLista : int
      +IdProveedor : int?
      +Nombre : string?
      +FechaCreacion : DateTime?
      +Observaciones : string?
      +IdUsuarioRegistra : int?
      +Activo : bool
      +IvaPorDefecto : decimal
      --
      +ObtenerPorProveedor(idProveedor) : List<ListaPrecioDTO>
      +ObtenerPorId(idLista) : ListaPrecioDTO
      +Crear(dto, idUsuario) : bool
      +Actualizar(dto) : bool
      +Eliminar(idLista) : bool
      +CambiarEstado(idLista) : bool
      +ObtenerItems(idLista) : List<ListaPrecioItemDTO>
      +AgregarItem(idLista, dto) : bool
      +ActualizarItem(idLista, idProducto, dto) : bool
      +EliminarItem(idLista, idProducto) : bool
      +DescargarPlantilla(idLista) : byte[]
      +ImportarItems(idLista, file, actualizarPrecioVenta, iva) : ImportResult
      +AgregarItemsMasivo(idLista, dto) : BulkAddResult
    }

    class ItemListaPrecio {
      +IdLista : int
      +IdProducto : int
      +Precio : decimal
      +Margen : decimal?
    }
  }

  ' =============================================
  ' USUARIOS Y PERMISOS
  ' =============================================
  package "Usuarios y Permisos" #d6eaf8 {
    class Usuario {
      +IdUsuario : int
      +Usuario1 : string
      +Password : string
      +Nombre : string
      +Apellido : string
      +Email : string
      +FechaAlta : DateOnly?
      +FechaBaja : DateOnly?
      +Rol : string
      +Root : bool
      --
      +Crear(dto) : bool
      +Actualizar(dto) : bool
      +CambiarContrasena(dto) : bool
      +Eliminar(id) : bool
      +Activar(id) : bool
      +ObtenerPaginado(pageIndex, pageSize, searchTerm, estado) : PagedList<UserDTO>
      +ObtenerTodos(id?) : List<UserDTO>
    }

    class Permiso {
      +IdPermiso : int
      +Permiso1 : string?
      +Descripcion : string?
      +IdCategoriaPermiso : int
      --
      +ObtenerPermisos(idCategoria?) : List<PermissionsCategoryDTO>
    }

    class CategoriaPermiso {
      +IdCategoriaPermiso : int
      +Categoria : string
    }

    class PermisoUsuario {
      +IdPermiso : int
      +IdUsuario : int
      +FechaAsignacion : DateOnly?
    }
  }

  ' =============================================
  ' PRODUCTOS
  ' =============================================
  package "Productos" #fdfefe {
    class Producto {
      +IdProducto : int
      +Nombre : string
      +Marca : string
      +Descripcion : string
      +Precio : decimal?
      +Costo : decimal
      +PorcentajeGanancia : decimal
      +Stock : decimal?
      +StockMinimo : decimal?
      +VentaSinStock : bool?
      +IdUbicacion : int?
      +IdCategoria : int?
      +IdUnidadMedida : int?
      +Activo : bool
      --
      +Crear(dto) : bool
      +Actualizar(dto) : bool
      +ObtenerTodos(activo) : List<ProductDetailDTO>
      +ObtenerPorId(id) : ProductDTO
      +Eliminar(id) : bool
      +CambiarEstado(id) : bool
      +ObtenerPaginado(pageIndex, pageSize, activo, search, idCategoria) :
  PagedList<ProductDetailDTO>
      +ExportarCsv() : byte[]
      +ExportarExcel() : byte[]
      +ImportarProductos(file, idUsuario) : BulkImportResult
      +DescargarPlantillaPrecios() : byte[]
      +ActualizarMasivoManual(dto) : ActualizarMasivoResult
      +ActualizarMasivoExcel(file, ivaDefecto) : ActualizarMasivoResult
    }

    class CodigoBarra {
      +IdCodigo : int
      +Codigo : string
      +Activo : bool?
      +Prinicial : bool?
      +IdProducto : int?
    }

    class MovimientoStock {
      +IdMovimientoStock : int
      +IdProducto : int
      +IdTipoMovimientoStock : int
      +Cantidad : decimal
      +StockResultante : decimal
      +Fecha : DateTime
      +IdUsuario : int?
      +Referencia : string?
      --
      +RegistrarMovimiento(idProducto, tipo, cantidad, referencia, idUsuario) : bool
      +ObtenerTipos() : List<TipoMovimientoStockDTO>
      +ObtenerTiposAdmin() : List<TipoMovimientoStockDTO>
      +ObtenerPaginado(idProducto, pageIndex, pageSize, idTipo) : PagedList<MovimientoStockDTO>
    }

    class TipoMovimientoStock {
      +IdTipoMovimientoStock : int
      +Nombre : string
      +Descripcion : string
      +Activo : bool
      +EsSistema : bool
      +EsPositivo : bool
      --
      +CrearTipo(dto) : TipoMovimientoStockDTO
      +ActualizarTipo(id, dto) : TipoMovimientoStockDTO
      +CambiarEstadoTipo(id) : bool
    }

    class Categoria {
      +IdCategoria : int
      +Categoria : string
      +Descripcion : string?
      --
      +Crear(dto) : bool
      +Actualizar(dto) : bool
      +ObtenerTodos() : List<CategoryDetailDTO>
      +ObtenerPorId(id) : CategoryDetailDTO
      +Eliminar(id) : bool
    }

    class Ubicacion {
      +IdUbicacion : int
      +Fila : int?
      +Seccion : string?
      +Nivel : int?
      +Activo : bool
      --
      +BuscarPaginado(pageIndex, pageSize, searchTerm, activos) : PagedList<LocationDTO>
      +ObtenerPorId(id) : LocationDTO
      +Crear(dto) : LocationDTO
      +Actualizar(id, dto) : LocationDTO
      +Eliminar(id) : bool
      +CambiarEstado(id) : bool
    }

    class UnidadMedida {
      +IdUnidadMedida : int
      +Nombre : string
      +Abreviatura : string
      +Activo : bool
      --
      +ObtenerTodas() : List<UnidadMedidaDTO>
      +ObtenerActivas() : List<UnidadMedidaDTO>
      +Crear(dto) : UnidadMedidaDTO
      +Actualizar(id, dto) : UnidadMedidaDTO
      +CambiarEstado(id) : bool
    }
  }

  ' =============================================
  ' RELACIONES
  ' =============================================

  ' --- Clientes ---
  Cliente "1" *-- "0..*" Venta
  Cliente "1" *-- "0..*" VentaPendiente
  Cliente "1" *-- "0..*" MovimientoCc

  ' --- Ventas internas ---
  Venta "1" *-- "1..*" DetalleVenta
  VentaPendiente "1" *-- "1..*" DetalleVentaPendiente
  DetalleVenta "0..*" --> "1" Producto
  DetalleVentaPendiente "0..*" --> "1" Producto
  VentaPendiente "0..*" --> "0..1" Venta
  MovimientoCc "0..*" --> "0..1" Venta

  ' --- Catálogos de Ventas ---
  Estado "1" o-- "0..*" Venta
  MedioPago "1" o-- "0..*" Venta
  MotivoNotaCredito "1" o-- "0..*" Venta
  Estado "1" o-- "0..*" VentaPendiente
  MedioPago "1" o-- "0..*" VentaPendiente

  ' --- Cuenta Corriente ---
  TipoMovimiento "1" o-- "0..*" MovimientoCc
  MotivoNotaDebito "1" o-- "0..*" MovimientoCc
  MotivoNotaCredito "1" o-- "0..*" MovimientoCc

  ' --- Usuarios ---
  Usuario "1" o-- "0..*" Venta
  Usuario "1" *-- "0..*" PermisoUsuario
  Permiso "1" *-- "0..*" PermisoUsuario
  CategoriaPermiso "1" o-- "0..*" Permiso
  Usuario "1" o-- "0..*" MovimientoCc
  Usuario "1" o-- "0..*" CompraProveedor
  Usuario "1" o-- "0..*" ListaPrecio

  ' --- Proveedores y Compras ---
  Proveedor "1" *-- "0..*" CompraProveedor
  CompraProveedor "1" *-- "1..*" CompraProveedorDetalle
  CompraProveedorDetalle "0..*" --> "1" Producto
  Proveedor "1" o-- "0..*" ListaPrecio
  ListaPrecio "1" *-- "0..*" ItemListaPrecio
  ItemListaPrecio "0..*" --> "1" Producto

  ' --- Productos ---
  Producto "1" *-- "0..*" CodigoBarra
  Producto "1" *-- "0..*" MovimientoStock
  Categoria "1" o-- "0..*" Producto
  Ubicacion "1" o-- "0..*" Producto
  UnidadMedida "1" o-- "0..*" Producto
  TipoMovimientoStock "1" o-- "0..*" MovimientoStock
  MovimientoStock "0..*" --> "0..1" Usuario

  @enduml

# Revision UML 
# ✅ Verificación del UML de Dominio (`uml2.md`)

---

## 1. Métodos faltantes por clase

### `MovimientoCc` — Faltan 8 métodos de `ICurrentAccountService`

| Método faltante | Qué hace |
|---|---|
| `AbrirCuenta(dto)` | Alta de cuenta corriente al cliente |
| `RegistrarPago(dto)` | Registrar pago (global, parcial, por factura) |
| `RegistrarNotaDebito(dto)` | Crear nota de débito |
| `AnularPago(dto)` | Anular un pago existente |
| `ObtenerClientesMorosos()` | Listar clientes con deuda vencida |
| `AplicarInteresCliente(clientId, userId)` | Aplicar ND de interés a un cliente |
| `AplicarInteresMasivo(userId)` | Aplicar interés a todos los morosos |
| `GenerarReciboPago(id)` | Generar recibo de pago en PDF |

### `Venta` — Faltan 3 métodos de `ISaleServices`

| Método faltante | Qué hace |
|---|---|
| `ExportarHistorialClienteExcel(idCliente, filtros)` | Excel con ventas de un cliente |
| `ExportarHistorialClientePdf(idCliente, filtros)` | PDF con ventas de un cliente |
| `ExportarComprobanteExcel(idVenta)` | Comprobante individual en Excel |

### `VentaPendiente` — Falta 1 método

| Método faltante | Qué hace |
|---|---|
| `ObtenerPorId(id)` | Detalle de una venta pendiente |

### `CompraProveedor` — Faltan 2 métodos

| Método faltante | Qué hace |
|---|---|
| `ExportarPorProveedorExcel(idProv, filtros)` | Excel historial por proveedor |
| `ExportarPorProveedorPdf(idProv, filtros)` | PDF historial por proveedor |

---

## 2. Relaciones faltantes

| Relación | Observación |
|---|---|
| `Usuario "1" o-- "0..*" VentaPendiente` | El vendedor y el autorizador son usuarios |
| `Estado "1" o-- "0..*" MovimientoCc` | MovimientoCc tiene FK a Estado (IdEstado) |

---

## 3. Análisis de relaciones — Composición vs Agregación vs Asociación

### Primero: ¿Qué es cada una?

| Tipo | Símbolo PlantUML | Significado | Ciclo de vida |
|---|---|---|---|
| **Composición** | `A "1" *-- "0..*" B` | B **es parte de** A. B no puede existir sin A | Si A se elimina, B se elimina |
| **Agregación** | `A "1" o-- "0..*" B` | A **tiene** B, pero B puede existir solo | Si A se elimina, B puede seguir existiendo |
| **Asociación** | `A "0..*" --> "1" B` | A **usa/referencia** B | Independientes entre sí |

### Análisis de cada relación del diagrama

#### ✅ Composiciones correctas (la parte NO existe sin el todo)

| Relación | Justificación |
|---|---|
| `Venta "1" *-- "1..*" DetalleVenta` | ✅ Los detalles son líneas de la venta. Sin venta no existen |
| `VentaPendiente "1" *-- "1..*" DetalleVentaPendiente` | ✅ Idem |
| `CompraProveedor "1" *-- "1..*" CompraProveedorDetalle` | ✅ Los renglones de la compra no existen solos |
| `ListaPrecio "1" *-- "0..*" ItemListaPrecio` | ✅ Los items pertenecen a la lista. Sin lista no tienen sentido |
| `Producto "1" *-- "0..*" CodigoBarra` | ✅ Un código de barra solo existe asociado a un producto |
| `Producto "1" *-- "0..*" MovimientoStock` | ✅ Los movimientos son el historial del producto. Sin producto no tienen razón de ser |

#### ✅ Agregaciones correctas (catálogos/tablas paramétricas)

| Relación | Justificación |
|---|---|
| `Estado "1" o-- "0..*" Venta` | ✅ Estado es un catálogo. Si eliminas "Vigente", las ventas siguen existiendo |
| `MedioPago "1" o-- "0..*" Venta` | ✅ Catálogo. Efectivo/CC son tipos independientes |
| `MotivoNotaCredito "1" o-- "0..*" Venta` | ✅ Catálogo |
| `Estado "1" o-- "0..*" VentaPendiente` | ✅ Catálogo |
| `MedioPago "1" o-- "0..*" VentaPendiente` | ✅ Catálogo |
| `TipoMovimiento "1" o-- "0..*" MovimientoCc` | ✅ Catálogo |
| `MotivoNotaDebito "1" o-- "0..*" MovimientoCc` | ✅ Catálogo |
| `MotivoNotaCredito "1" o-- "0..*" MovimientoCc` | ✅ Catálogo |
| `CategoriaPermiso "1" o-- "0..*" Permiso` | ✅ Catálogo de agrupación |
| `Categoria "1" o-- "0..*" Producto` | ✅ Catálogo |
| `Ubicacion "1" o-- "0..*" Producto` | ✅ Catálogo |
| `UnidadMedida "1" o-- "0..*" Producto` | ✅ Catálogo |
| `TipoMovimientoStock "1" o-- "0..*" MovimientoStock` | ✅ Catálogo |

#### ✅ Asociaciones correctas (referencia sin propiedad)

| Relación | Justificación |
|---|---|
| `DetalleVenta "0..*" --> "1" Producto` | ✅ El detalle referencia al producto, no lo posee |
| `DetalleVentaPendiente "0..*" --> "1" Producto` | ✅ Idem |
| `VentaPendiente "0..*" --> "0..1" Venta` | ✅ La pendiente puede generar una venta o no |
| `MovimientoCc "0..*" --> "0..1" Venta` | ✅ El movimiento puede referenciar una venta |
| `CompraProveedorDetalle "0..*" --> "1" Producto` | ✅ Referencia |
| `ItemListaPrecio "0..*" --> "1" Producto` | ✅ Referencia |
| `MovimientoStock "0..*" --> "0..1" Usuario` | ✅ Referencia al usuario que registró |

#### ✅ Agregaciones correctas (usuario como referencia)

| Relación | Justificación |
|---|---|
| `Usuario "1" o-- "0..*" Venta` | ✅ El usuario vendedor. El usuario existe independiente de sus ventas |
| `Usuario "1" o-- "0..*" MovimientoCc` | ✅ Idem |
| `Usuario "1" o-- "0..*" CompraProveedor` | ✅ Idem |
| `Usuario "1" o-- "0..*" ListaPrecio` | ✅ Idem |

#### ⚠️ Composiciones a revisar

| Relación actual | Problema | Corrección sugerida |
|---|---|---|
| `Cliente "1" *-- "0..*" Venta` | Una venta es un registro histórico/fiscal. Si das de baja un cliente, las ventas **no se eliminan** (soft delete). Además, la venta existe como documento independiente. | `Cliente "1" o-- "0..*" Venta` (agregación) |
| `Cliente "1" *-- "0..*" VentaPendiente` | Mismo razonamiento que Venta | `Cliente "1" o-- "0..*" VentaPendiente` (agregación) |
| `Cliente "1" *-- "0..*" MovimientoCc` | Los movimientos CC son registros contables. No se eliminan al dar de baja un cliente | `Cliente "1" o-- "0..*" MovimientoCc` (agregación) |
| `Proveedor "1" *-- "0..*" CompraProveedor` | Las compras son registros fiscales independientes. Si desactivas el proveedor, las compras históricas persisten | `Proveedor "1" o-- "0..*" CompraProveedor` (agregación) |
| `Permiso "1" *-- "0..*" PermisoUsuario` | No puede haber composición desde **ambos lados** (Usuario y Permiso). PermisoUsuario es una tabla intermedia M:N. El usuario "posee" sus asignaciones, el permiso solo es referenciado | `Permiso "1" --> "0..*" PermisoUsuario` (asociación) |
| `Usuario "1" *-- "0..*" PermisoUsuario` | ✅ Esta sí está bien como composición. Las asignaciones pertenecen al usuario | Mantener `*--` |

### Resumen visual de correcciones en relaciones

```diff
  ' --- Clientes ---
- Cliente "1" *-- "0..*" Venta
+ Cliente "1" o-- "0..*" Venta
- Cliente "1" *-- "0..*" VentaPendiente
+ Cliente "1" o-- "0..*" VentaPendiente
- Cliente "1" *-- "0..*" MovimientoCc
+ Cliente "1" o-- "0..*" MovimientoCc

  ' --- Proveedores ---
- Proveedor "1" *-- "0..*" CompraProveedor
+ Proveedor "1" o-- "0..*" CompraProveedor

  ' --- Permisos ---
  Usuario "1" *-- "0..*" PermisoUsuario          (mantener composición)
- Permiso "1" *-- "0..*" PermisoUsuario
+ Permiso "1" --> "0..*" PermisoUsuario           (cambiar a asociación)

  ' --- Agregar relaciones faltantes ---
+ Usuario "1" o-- "0..*" VentaPendiente
+ Estado "1" o-- "0..*" MovimientoCc
```

> [!TIP]
> **Regla práctica para decidir:**
> - ¿Si elimino A, B pierde todo sentido? → **Composición** (ej: Venta → DetalleVenta)
> - ¿A "tiene" B pero B es un registro que persiste solo? → **Agregación** (ej: Cliente → Venta)
> - ¿A solo "referencia" B sin poseerlo? → **Asociación** (ej: DetalleVenta → Producto)
> - ¿B es un catálogo/tabla paramétrica? → **Agregación** siempre
