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
    +Nombre : string
    +Apellido : string
    +RazonSocial : string
    +Cuit : string
    +Dni : string
    +Telefono : string
    +Mail : string
    +FechaAlta : DateOnly
    +FechaBaja : DateOnly
    --
    +Crear(dto) : Cliente
    +ObtenerPorId(id) : Cliente
    +BuscarPaginado(filtros) : List~Cliente~
    +Actualizar(dto) : Cliente
    +CambiarEstado(dto) : string
  }
}

' =============================================
' CUENTA CORRIENTE
' =============================================
package "Cuenta Corriente" #fef9e7 {
  class MovimientoCc {
    +IdMovimiento : int
    +Importe : decimal
    +Fecha : DateTime
    +Detalle : string
    +SaldoActual : decimal
    +LimiteCuenta : decimal
    +IdTipoMovimiento : int
    +IdCliente : int
    +IdUsuarioRegistra : int
    +IdUsuarioAutoriza : int
    +IdVenta : int
    +MontoPagado : decimal
    +EsAnulado : bool
    +IdMotivoNd : int
    +IdMotivoNc : int
    --
    +AbrirCuenta(dto) : bool
    +ObtenerResumen(clientId) : MovimientoCc
    +ObtenerMovimientosPaginado(clientId, filtros) : List~MovimientoCc~
    +ObtenerHistorialLimite(clientId) : List~MovimientoCc~
    +ActualizarLimite(dto) : bool
    +RegistrarPago(dto) : bool
    +RegistrarNotaDebito(dto) : bool
    +AnularPago(dto) : bool
    +ObtenerClientesMorosos() : List~Cliente~
    +AplicarInteresCliente(clientId, userId) : bool
    +AplicarInteresMasivo(userId) : bool
    +GenerarReciboPago(id) : byte[]
    +ExportarPdf(clientId) : byte[]
    +ExportarExcel(clientId) : byte[]
  }

  class TipoMovimiento {
    +IdMovimiento : int
    +Nombre : string
    +Accion : string
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
    +Fecha : DateTime
    +Total : decimal
    +IdMedioPago : int
    +IdCliente : int
    +IdUsuario : int
    +IdEstado : int
    +IdMotivoNc : int
    +DetalleNc : string
    --
    +Crear(dto) : Venta
    +ObtenerPaginado(filtros) : List~Venta~
    +ObtenerPorId(id) : Venta
    +Anular(id, dto) : bool
    +ExportarComprobantesPdf(fechaDesde, fechaHasta) : byte[]
    +ExportarComprobantesExcel(fechaDesde, fechaHasta) : byte[]
    +ExportarComprobanteExcel(idVenta) : byte[]
    +ExportarNotaCreditoPdf(id) : byte[]
    +ExportarHistorialClienteExcel(idCliente, filtros) : byte[]
    +ExportarHistorialClientePdf(idCliente, filtros) : byte[]
  }

  class VentaPendiente {
    +IdVentaPendiente : int
    +CodigoVenta : string
    +IdCliente : int
    +IdUsuarioVendedor : int
    +IdMedioPago : int
    +Total : decimal
    +SaldoActual : decimal
    +LimiteCuenta : decimal
    +SaldoDespuesVenta : decimal
    +Excedente : decimal
    +IdEstado : int
    +IdUsuarioAutoriza : int
    +FechaAutorizacion : DateTime
    +ObservacionesAutorizacion : string
    +FechaRegistro : DateTime
    +IdVentaGenerada : int
    --
    +ObtenerPaginado(pageIndex, pageSize, searchTerm) : List~VentaPendiente~
    +ObtenerPorId(id) : VentaPendiente
    +Aprobar(id, dto) : bool
    +Rechazar(id, dto) : bool
  }

  class DetalleVenta {
    +IdVenta : int
    +IdProducto : int
    +Cantidad : decimal
    +PrecioVenta : decimal
    +SubTotal : decimal
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
    +MedioPago1 : string
  }

  class Estado {
    +IdEstado : int
    +Estado1 : string
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
    +Direccion : string
    +Telefono : string
    +Activo : bool
    +FechaBaja : DateTime
    --
    +Crear(dto) : bool
    +Actualizar(dto) : bool
    +ObtenerTodos() : List~Proveedor~
    +ObtenerPorId(id) : Proveedor
    +Eliminar(id) : bool
    +CambiarEstado(id) : bool
    +ObtenerPaginado(filtros) : List~Proveedor~
    +ExportarExcel() : byte[]
    +ExportarPdf() : byte[]
  }

  class CompraProveedor {
    +IdCompraProveedor : int
    +IdProveedor : int
    +Fecha : DateOnly
    +FechaVencimiento : DateOnly
    +TipoComprobante : string
    +NumeroComprobante : string
    +Observacion : string
    +Subtotal : decimal
    +DescuentoTotal : decimal
    +IvaTotal : decimal
    +Total : decimal
    +IdUsuario : int
    +Activo : bool
    --
    +Crear(dto) : CompraProveedor
    +ObtenerPaginado(filtros) : List~CompraProveedor~
    +ObtenerPorProveedor(idProveedor, filtros) : List~CompraProveedor~
    +ObtenerPorId(id) : CompraProveedor
    +Anular(id, dto) : bool
    +ExportarListadoExcel(fechaDesde, fechaHasta) : byte[]
    +ExportarListadoPdf(fechaDesde, fechaHasta) : byte[]
    +ExportarCompraExcel(id) : byte[]
    +ExportarCompraPdf(id) : byte[]
    +ExportarPorProveedorExcel(idProv, filtros) : byte[]
    +ExportarPorProveedorPdf(idProv, filtros) : byte[]
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
    +IdProveedor : int
    +Nombre : string
    +FechaCreacion : DateTime
    +Observaciones : string
    +IdUsuarioRegistra : int
    +Activo : bool
    +IvaPorDefecto : decimal
    --
    +ObtenerPorProveedor(idProveedor) : List~ListaPrecio~
    +ObtenerPorId(idLista) : ListaPrecio
    +Crear(dto, idUsuario) : bool
    +Actualizar(dto) : bool
    +Eliminar(idLista) : bool
    +CambiarEstado(idLista) : bool
    +ObtenerItems(idLista) : List~ItemListaPrecio~
    +AgregarItem(idLista, dto) : bool
    +ActualizarItem(idLista, idProducto, dto) : bool
    +EliminarItem(idLista, idProducto) : bool
    +DescargarPlantilla(idLista) : byte[]
    +ImportarItems(idLista, filtros) : bool
    +AgregarItemsMasivo(idLista, dto) : bool
  }

  class ItemListaPrecio {
    +IdLista : int
    +IdProducto : int
    +Precio : decimal
    +Margen : decimal
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
    +FechaAlta : DateOnly
    +FechaBaja : DateOnly
    +Rol : string
    +Root : bool
    --
    +Crear(dto) : bool
    +Actualizar(dto) : bool
    +CambiarContrasena(dto) : bool
    +Eliminar(id) : bool
    +Activar(id) : bool
    +ObtenerPaginado(filtros) : List~Usuario~
    +ObtenerTodos(id) : List~Usuario~
  }

  class Permiso {
    +IdPermiso : int
    +Permiso1 : string
    +Descripcion : string
    +IdCategoriaPermiso : int
    --
    +ObtenerPermisos(idCategoria) : List~Permiso~
  }

  class CategoriaPermiso {
    +IdCategoriaPermiso : int
    +Categoria : string
  }

  class PermisoUsuario {
    +IdPermiso : int
    +IdUsuario : int
    +FechaAsignacion : DateOnly
  }
}

' =============================================
' PRODUCTOS
' =============================================
package "Productos" #eeeeee {
  class Producto {
    +IdProducto : int
    +Nombre : string
    +Marca : string
    +Descripcion : string
    +Precio : decimal
    +Costo : decimal
    +PorcentajeGanancia : decimal
    +Stock : decimal
    +StockMinimo : decimal
    +VentaSinStock : bool
    +IdUbicacion : int
    +IdCategoria : int
    +IdUnidadMedida : int
    +Activo : bool
    --
    +Crear(dto) : bool
    +Actualizar(dto) : bool
    +ObtenerTodos(activo) : List~Producto~
    +ObtenerPorId(id) : Producto
    +Eliminar(id) : bool
    +CambiarEstado(id) : bool
    +ObtenerPaginado(filtros) : List~Producto~
    +ExportarCsv() : byte[]
    +ExportarExcel() : byte[]
    +ImportarProductos(file, idUsuario) : bool
    +DescargarPlantillaPrecios() : byte[]
    +ActualizarMasivoManual(dto) : bool
    +ActualizarMasivoExcel(file, ivaDefecto) : bool
  }

  class CodigoBarra {
    +IdCodigo : int
    +Codigo : string
    +Activo : bool
    +Prinicial : bool
    +IdProducto : int
  }

  class MovimientoStock {
    +IdMovimientoStock : int
    +IdProducto : int
    +IdTipoMovimientoStock : int
    +Cantidad : decimal
    +StockResultante : decimal
    +Fecha : DateTime
    +IdUsuario : int
    +Referencia : string
    --
    +RegistrarMovimiento(idProducto, filtros) : bool
    +ObtenerTipos() : List~TipoMovimientoStock~
    +ObtenerTiposAdmin() : List~TipoMovimientoStock~
    +ObtenerPaginado(idProducto, filtros) : List~MovimientoStock~
  }

  class TipoMovimientoStock {
    +IdTipoMovimientoStock : int
    +Nombre : string
    +Descripcion : string
    +Activo : bool
    +EsSistema : bool
    +EsPositivo : bool
    --
    +CrearTipo(dto) : TipoMovimientoStock
    +ActualizarTipo(id, dto) : TipoMovimientoStock
    +CambiarEstadoTipo(id) : bool
  }

  class Categoria {
    +IdCategoria : int
    +Categoria : string
    +Descripcion : string
    --
    +Crear(dto) : bool
    +Actualizar(dto) : bool
    +ObtenerTodos() : List~Categoria~
    +ObtenerPorId(id) : Categoria
    +Eliminar(id) : bool
  }

  class Ubicacion {
    +IdUbicacion : int
    +Fila : int
    +Seccion : string
    +Nivel : int
    +Activo : bool
    --
    +BuscarPaginado(filtros) : List~Ubicacion~
    +ObtenerPorId(id) : Ubicacion
    +Crear(dto) : Ubicacion
    +Actualizar(id, dto) : Ubicacion
    +Eliminar(id) : bool
    +CambiarEstado(id) : bool
  }

  class UnidadMedida {
    +IdUnidadMedida : int
    +Nombre : string
    +Abreviatura : string
    +Activo : bool
    --
    +ObtenerTodas() : List~UnidadMedida~
    +ObtenerActivas() : List~UnidadMedida~
    +Crear(dto) : UnidadMedida
    +Actualizar(id, dto) : UnidadMedida
    +CambiarEstado(id) : bool
  }
}

' =============================================
' RELACIONES
' =============================================

' --- Clientes ---
Cliente "1" o-- "0..*" Venta
Cliente "1" o-- "0..*" VentaPendiente
Cliente "1" o-- "0..*" MovimientoCc

' --- Ventas internas ---
Venta "1" *-- "1..*" DetalleVenta
VentaPendiente "1" *-- "1..*" DetalleVentaPendiente
DetalleVenta "0..*" --> "1" Producto
DetalleVentaPendiente "0..*" --> "1" Producto
VentaPendiente "0..*" --> "0..1" Venta
MovimientoCc "0..*" --> "0..1" Venta

' --- Catalogos de Ventas ---
Estado "1" o-- "0..*" Venta
MedioPago "1" o-- "0..*" Venta
MotivoNotaCredito "1" o-- "0..*" Venta
Estado "1" o-- "0..*" VentaPendiente
MedioPago "1" o-- "0..*" VentaPendiente

' --- Cuenta Corriente ---
TipoMovimiento "1" o-- "0..*" MovimientoCc
MotivoNotaDebito "1" o-- "0..*" MovimientoCc
MotivoNotaCredito "1" o-- "0..*" MovimientoCc
Estado "1" o-- "0..*" MovimientoCc

' --- Usuarios ---
Usuario "1" o-- "0..*" Venta
Usuario "1" *-- "0..*" PermisoUsuario
Permiso "1" --> "0..*" PermisoUsuario
CategoriaPermiso "1" o-- "0..*" Permiso
Usuario "1" o-- "0..*" MovimientoCc
Usuario "1" o-- "0..*" CompraProveedor
Usuario "1" o-- "0..*" ListaPrecio
Usuario "1" o-- "0..*" VentaPendiente

' --- Proveedores y Compras ---
Proveedor "1" o-- "0..*" CompraProveedor
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
