# Refactor: Sincronización de Costos, Margen de Ganancia y Precios de Venta

## 1. Contexto Actual
El proyecto cuenta con un módulo de **Productos** que incluye una entidad `Producto` con un único campo: `Precio` (precio final de venta al mostrador). Existen dos módulos adicionales que alimentan el negocio:

- **Listas de Precios (Excel)**: Los proveedores envían listas, usualmente con precios netos (sin IVA). El ferretero carga un `precio` y un `margen`.

- **Compras (Proveedores)**: El ferretero carga la factura del proveedor con un `Precio unitario`, un `% IVA` y descuentos.

Actualmente, el valor de entrada de mercadería de ambos flujos no guarda registro histórico del costo directo y desaprovecha el conocimiento impositivo generado en el comprobante real, dejando el producto ciego.

## 2. Problema (La Desconexión e Imperfección Tributaria)
Actualmente, el sistema carece de la resiliencia y trazabilidad requerida en la vida real:
1. **La entidad `Producto` ignora su costo real**. Ignora el IVA que paga el ferretero.
2. **Definición de Costo Minorista**: Para la ferretería pequeña, el "Costo Real" del producto es lo que terminó pagando (Neto + IVA - Descuentos), ya que muchos no descargan IVA a la hora de revender.
3. El módulo de **Compras** es ciego frente al precio. Cargar una compra más cara no repercute en el precio de venta en mostrador.
4. Las **Listas de Excel** y las **Compras** no se ponen de acuerdo en el tratamiento de impuestos estáticos y dinámicos para proteger el margen de ganancia.

## 3. Solución (Estandarización y Prevención de Obsolescencia - Opción A)
Para convertir este sistema en un ERP profesional escalable, todo `Producto` debe ser dueño de su triada financiera: `Costo`, `PorcentajeGanancia`, y `Precio`.
- **Costo**: Representa el valor base real y final del ítem con el **IVA INCLUIDO**. 
- **PorcentajeGanancia**: Es la cuota estática fijada en el producto (ej: 30%). 
- **Precio**: Es el PVP del mostrador. Siempre debe respetarse la fórmula: `Precio = Costo + (Costo * PorcentajeGanancia / 100)`.

A partir de este refactor:
1. **Las Compras físicas** utilizarán el input manual numérico de IVA (que ya ha sido liberado de sus validaciones restrictivas de select) más una nueva columna de "Margen (%)" que el usuario podrá definir fila por fila para proteger o alterar el margen de cada producto individual in-situ durante la compra.
2. **Los Excel de Listas** se asumirán como **PRECIOS NETOS (Sin IVA)**. Para evitar la obsolescencia si el IVA país cambia en el futuro, no se "harcodeará" el 21% en el backend. En su lugar, el Modal de carga en React preguntará explícitamente "Qué alícuota de IVA aplica transversalmente a los netos de este Excel". 

---

## 4. Spec para Implementación Necesaria

> **Atención Claude:** Implementar las siguientes directivas matemáticas asumiendo un diseño de escalabilidad a futuro y asegurando integridad transaccional (EF Core Transactions en todos los DbContext).

### A. Capa de Datos (`Models/Producto.cs` y DTOs)
1. **Migrations / EF Core**: 
   - Añadir a `Producto.cs`: `public decimal Costo { get; set; } = 0;`
   - Añadir a `Producto.cs`: `public decimal PorcentajeGanancia { get; set; } = 30;` // (Puede usarse un 30% como default conservador).
   - Generar la EF Migration (`AddCostoGananciaToProduct`) y actualizar la DB.
2. **DTOs**:
   - Actualizar `ProductDTO`, y formularios correspondientes para soportar estos nuevos campos en el CRUD normal.

### B. Lógica en Compras (`Features/CompraProveedor/Services/CompraProveedorServices.cs`)
1. **Nuevo Input en la Grilla de UI y Pre-carga Inteligente (`CompraForm.jsx`)**:
   - Crear una nueva columna para un Input numérico "Margen %" (ej: `det.margenAplicado`) y transmitirlo en el DTO (`CompraProveedorCreateDTO`).
   - *Regla UX de Pre-carga:* Si el producto es añadido al carrito haciendo click desde el panel lateral "Lista de Precios" (vía `agregarDesdeLista()`), dicho Input de margen debe pre-cargarse automáticamente con el margen ya configurado en esa lista (`item.margen`). Si se agrega por búsqueda libre central, pre-cargar con el margen base del producto. En todos los casos el field conserva la permisividad para ser sobreescripto.
2. **Resolución de Costos Reales con IVA Dinámico**:
   - Al iterar los detalles `dto.Detalles`, obtener el costo base aplicando el IVA libre que ya viaja en el payload.
   - Fórmula en memoria: `CostoRealFacturado = ((PrecioUnitario * (1 - (DescuentoPorcentaje / 100))) * (1 + (IvaPorcentaje / 100)))`.
3. **Actualización Absoluta o Condicional de Precios**:
   - Si el usuario suministró un nuevo `Margen %` en la grilla para esa fila, **siempre** asignarlo a `producto.PorcentajeGanancia` (incluso si el costo bajó o se mantuvo, ya que la voluntad del usuario prevalece).
   - Validar si `CostoRealFacturado > productoActual.Costo`, o bien si el margen fue modificado a mano; si ocurre cualquiera de las dos, grabar el nuevo costo y derivar la re-escritura del PVP de la entidad principal:
     - `producto.Costo = CostoRealFacturado;`
     - `producto.Precio = producto.Costo * (1 + (producto.PorcentajeGanancia / 100));`
4. **Guardado Atómico**: 
   - Realizar los _Updates_ correspondientes invocando `SaveChangesAsync()` dentro del scope seguro de la BD (`using var transaction`).

### C. Lógica en Importador de Listado Excel 
Aquí resolveremos la escalabilidad del IVA sin hardcodearlo, aprovechando inteligentemente la decisión de diseño previamente tomada: ¡El objeto `ListaPrecio` ya posee la propiedad base `IvaPorDefecto`!

1. **Lectura Inteligente desde la Cabecera de la Lista**: 
   - No es necesario inyectar nuevos modales en React. El backend sabe en todo momento para qué `idLista` estamos importando el Excel.
   - Antes de iterar las filas del Excel en `Features/ListaPrecio/Services/ListaPrecioService.cs`, obtener la entidad `ListaPrecio` principal desde el Repositorio y leer su `IvaPorDefecto` (ej: 21).

2. **Cálculo y Guardado Atómico (Backend)**:
   - Asumimos que el precio declarado en el Excel es **Neto**. Aplicamos la inflación tributaria pre-cargada en la Lista para normalizarlo y obtener el Costo General.
   - Fórmula en memoria para cada fila parseada del Excel: 
     - `CostoFinalConIva = PrecioNetoExcel * (1 + (listaPrecio.IvaPorDefecto / 100));`
     - Recalculamos el PVP sobre la marcha combinándolo con el margen provisto en dicha fila: `NuevoPrecio = CostoFinalConIva * (1 + (MargenExcel / 100));`.
   - Propagar la asimilación escribiendo tanto sobre la pivot `ProductoListaprecioProveedor` como en las maestras `Producto.Costo` y `Producto.Precio` (siempre que decidan mantener sincronismos de forma bidireccional), invocando el log de persistencia mediante EF Core Transactions.

---

## 5. Implementación Realizada

### Visión General

Se implementó el refactor completo en dos grandes frentes — backend (ASP.NET Core / EF Core) y frontend (React) — respetando la arquitectura por capas existente del proyecto (Controllers → Services → Repositories → DTOs).

---

### A. Modelo de Datos

**`Models/Producto.cs`**
Se agregaron dos nuevas propiedades a la entidad `Producto`:
- `Costo` (decimal, default 0): representa el costo real del producto con IVA incluido.
- `PorcentajeGanancia` (decimal, default 30): el margen fijo configurado en el producto.

Se generó y aplicó la migración `AddCostoGananciaToProduct`, que ejecuta dos `ALTER TABLE` sobre la tabla `producto` en PostgreSQL.

**`Features/Product/DTO/ProductDTO.cs`**
Se actualizaron `ProductDTO` y `ProductDetailDTO` con los dos nuevos campos. El método `UpdateAsync` de `ProductServices.cs` fue extendido para asignar manualmente `Costo` y `PorcentajeGanancia` en el bloque de edición, siguiendo el patrón existente del proyecto (asignación manual en Update, sin AutoMapper).

---

### B. Módulo de Compras

**`Features/CompraProveedor/DTO/CompraProveedorCreateDTO.cs`**
Se agregó `decimal? MargenAplicado` al DTO de detalle de compra (`CompraProveedorDetalleCreateDTO`).

**`Features/CompraProveedor/Services/CompraProveedorServices.cs`**
Dentro del flujo de creación de una compra, se añadió un bloque de actualización de producto que:
1. Calcula `CostoRealFacturado = PrecioUnitario * (1 - Descuento/100) * (1 + IVA/100)`.
2. Si el usuario proporcionó un `MargenAplicado` para esa línea, lo asigna siempre a `producto.PorcentajeGanancia` (la voluntad del usuario prevalece).
3. Si el costo facturado es mayor al costo actual del producto, o si el margen fue modificado, actualiza `producto.Costo` y recalcula `producto.Precio = Costo * (1 + PorcentajeGanancia / 100)`.
4. Todos los cambios se guardan dentro de la transacción existente de la compra.

**`CompraForm.jsx`** (React)
Se agregó la columna "Margen %" al carrito de la compra:
- Productos agregados desde una lista de precios se pre-cargan con el margen de esa lista (`item.margen`).
- Productos agregados por búsqueda libre se pre-cargan con el `porcentajeGanancia` del producto.
- El campo es siempre editable para que el ferretero pueda sobreescribirlo fila por fila.

---

### C. Módulo de Listas de Precios — IVA por Defecto

Adicionalmente a la spec original, se resolvió un problema de escalabilidad más profundo: en lugar de pedir el IVA en cada importación Excel puntual, se implementó un **IVA a nivel de lista** que se configura una sola vez y aplica en todos los flujos de esa lista.

**`Models/ListaPrecio.cs`**
Se agregó `IvaPorDefecto` (decimal, default 21) a la entidad `ListaPrecio`. Se generó y aplicó la migración `AddIvaPorDefectoToListaPrecio`. Las listas existentes reciben automáticamente 21% como valor inicial.

**`Features/ListaPrecio/DTO/ListaPrecioDTO.cs`**
Se actualizaron `ListaPrecioDTO`, `ListaPrecioCreateDTO` y `ListaPrecioUpdateDTO` con el nuevo campo.

**`Features/ListaPrecio/Services/ListaPrecioItemServices.cs`**
- `AddItemAsync` y `UpdateItemAsync`: refactorizados para usar transacciones. Al cargar o editar un ítem manualmente, aplican el IVA de la lista sobre el precio neto ingresado (`costoConIva = precio * (1 + iva/100)`) y actualizan `Producto.Costo` y `Producto.Precio` con la misma lógica que la importación masiva.
- `ImportarAsync`: si `ivaAplicacion == 0` (no se envió desde el frontend), toma automáticamente `lista.IvaPorDefecto` como fallback.

**Frontend — Listas de Precios**
- El dialog de crear/editar lista (`ProveedorDetailsPage.jsx`) ahora incluye el campo "IVA por defecto (%)" con valor inicial 21.
- El card de detalle de la lista (`ListaDetailsPage.jsx`) muestra el IVA configurado.
- El modal de carga masiva (`BulkPriceImportModal.jsx`) recibe el IVA como prop y lo muestra en pantalla de forma informativa, sin requerir que el usuario lo reingrese cada vez.

---

### D. Formulario de Producto

**`product-form.jsx`** (React)
Se agregaron los campos Costo y Margen al formulario de alta/edición de producto, con lógica de cálculo cruzado reactivo:
- Modificar **Costo** o **Margen** recalcula el **Precio** automáticamente.
- Modificar el **Precio** directamente retrocede el cálculo del **Margen** (`Margen = (Precio/Costo - 1) * 100`), siempre que `Costo > 0`.
- Los tres campos se presentan en una sola fila en el orden: Costo → Margen → Precio de venta.

---

### Resumen de Archivos Modificados

| Capa | Archivo | Cambio |
|---|---|---|
| Modelo | `Models/Producto.cs` | + `Costo`, `PorcentajeGanancia` |
| Modelo | `Models/ListaPrecio.cs` | + `IvaPorDefecto` |
| Migración | `AddCostoGananciaToProduct` | `ALTER TABLE producto ADD Costo, PorcentajeGanancia` |
| Migración | `AddIvaPorDefectoToListaPrecio` | `ALTER TABLE lista_precio ADD IvaPorDefecto` |
| DTO | `ProductDTO`, `ProductDetailDTO` | + `Costo`, `PorcentajeGanancia` |
| DTO | `ListaPrecioDTO/CreateDTO/UpdateDTO` | + `IvaPorDefecto` |
| DTO | `CompraProveedorDetalleCreateDTO` | + `MargenAplicado` |
| Service | `ProductServices.UpdateAsync` | Asigna `Costo`, `PorcentajeGanancia` |
| Service | `CompraProveedorServices.Create` | Calcula `CostoRealFacturado`, actualiza producto |
| Service | `ListaPrecioServices.UpdateAsync` | Guarda `IvaPorDefecto` |
| Service | `ListaPrecioItemServices` | `Add/UpdateItemAsync` con IVA; `ImportarAsync` con fallback a lista |
| Frontend | `product-form.jsx` | Campos Costo, Margen, Precio con cálculo cruzado |
| Frontend | `CompraForm.jsx` | Columna Margen % en carrito con pre-carga inteligente |
| Frontend | `ProveedorDetailsPage.jsx` | Campo IVA en dialog crear/editar lista |
| Frontend | `ListaDetailsPage.jsx` | Muestra IVA; pasa prop a `BulkPriceImportModal` |
| Frontend | `BulkPriceImportModal.jsx` | Muestra IVA configurado de forma informativa |
