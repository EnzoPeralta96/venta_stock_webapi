# Compras a Proveedores — Funcionamiento del Sistema

## Conceptos clave

El módulo de compras a proveedores tiene tres conceptos separados:

| Concepto | Qué es | Para qué sirve |
|---|---|---|
| **Lista de Precios** | Catálogo de referencia del proveedor | Registrar qué cobra el proveedor por cada producto |
| **Compra** | Transacción real de mercadería | Registrar una compra, actualizar stock y costos |
| **Actualizar Precios** | Acción operativa directa | Actualizar masivamente el costo y precio de venta de productos |

Estos tres conceptos son **independientes** entre sí. No es obligatorio tener una lista de precios para hacer una compra, ni hacer una compra para actualizar precios.

---

## Opción 1 — Compra directa sin lista de precios

**Flujo:**
```
Proveedor → Ver detalles → Compras → Realizar compra
```

1. Se completan los datos del encabezado: proveedor (pre-cargado), fecha, fecha de vencimiento, tipo de comprobante, número, observaciones.
2. Se agregan productos buscando por nombre o marca.
3. Por cada producto se ingresa: cantidad, precio unitario (neto), descuento %, IVA %, margen % (opcional).
4. Se registra la compra.

**Al registrar:**
- El stock de cada producto se incrementa.
- Si el nuevo costo (precio neto con IVA aplicado) supera al costo actual del producto → se actualiza `Costo` y se recalcula `Precio`.
- Si se ingresó un margen → también se actualiza `PorcentajeGanancia`.

---

## Opción 2 — Compra usando lista de precios como referencia

**Flujo:**
```
Proveedor → Ver detalles → Lista de precios → [Realizar compra]
  → Formulario de compra con lista pre-seleccionada
```

1. Desde la página de detalle de una lista de precios, el botón **"Realizar compra"** lleva directamente al formulario de compra con la lista pre-seleccionada.
2. En el formulario aparece el panel **"Productos de la lista"** con los productos catalogados y sus costos de referencia.
3. Se agregan los productos al carrito haciendo clic en **"Agregar"** → el precio unitario y el IVA se pre-cargan desde la lista.
4. Se pueden agregar productos adicionales que no estén en la lista usando el buscador externo.
5. Se registra la compra con el mismo resultado que la Opción 1.

**Nota:** La lista de precios es solo una **referencia de carga rápida**. El precio que se graba en la compra es el que queda en el carrito (editable), no el de la lista.

---

## Lista de Precios — Gestión del catálogo

**Acceso:**
```
Proveedor → Ver detalles → Tab "Listas de Precios"
```

### Crear una lista
- Nombre, observaciones, IVA por defecto (%).
- Una lista representa una entrega/cotización del proveedor en un momento dado. Se crea una lista nueva por cada cotización recibida para mantener historial.

### Agregar productos a la lista
- Botón **"Agregar producto"** → abre un dialog de carga multiple.
- Se busca el producto, se ingresa el **costo neto sin IVA**, se hace clic en "Agregar" → pasa al staging.
- Se pueden agregar varios productos antes de guardar.
- Al guardar, el sistema aplica el IVA por defecto de la lista: `costoConIva = costoNeto × (1 + IVA/100)`.
- La lista **nunca modifica** los precios del producto — es solo un catálogo de referencia.

### Editar / eliminar items
- Cada item tiene botones de editar precio y eliminar.
- Al editar se muestra el costo neto actual (se revierte el IVA para mostrarlo sin IVA).

---

## Actualizar Precios — Actualización masiva

**Acceso:**
```
Proveedor → Ver detalles → [Actualizar precios] (botón en el header)
```

Acción operativa separada de las listas de precios y de las compras. Permite actualizar el costo y precio de venta de productos directamente, cuando el proveedor comunica nuevos precios sin que exista una compra real.

### Flujo
1. Clic en **"Actualizar precios"** desde cualquier tab del proveedor.
2. Se buscan y agregan los productos a actualizar.
3. Por cada producto se ingresa:
   - **Costo neto** (sin IVA) — obligatorio
   - **IVA %** — default 21%, editable
   - **Margen %** — pre-cargado con el margen actual del producto, editable
4. El modal muestra en tiempo real: **Costo c/IVA** calculado y **Precio de venta** resultante.
5. Al guardar, por cada producto:
   - `Costo = costoNeto × (1 + IVA/100)`
   - `PorcentajeGanancia = margenIngresado` (si se modificó)
   - `Precio = Costo × (1 + PorcentajeGanancia/100)`

### Resultado
Tras guardar, el modal muestra una tabla con el **antes y después** de cada producto:

| Producto | Costo anterior | Costo nuevo | Precio anterior | Precio nuevo |
|---|---|---|---|---|
| Pintura 1L | $100.00 | $121.00 | $130.00 | $157.30 |

### Auditoría
Cada actualización masiva queda registrada en el log de auditoría con:
- Usuario y fecha/hora
- Proveedor desde el que se realizó
- Valores anteriores y nuevos de cada producto

---

## Comportamiento del costo y precio al registrar una compra

```
costoReal = precioUnitario × (1 - descuento/100) × (1 + IVA/100)

Si costoReal > producto.Costo  →  actualiza Costo
Si se ingresó margen            →  actualiza PorcentajeGanancia
Si cualquiera de los dos cambió →  recalcula Precio = Costo × (1 + PorcentajeGanancia/100)
```

**Regla:** si el nuevo costo es menor o igual al actual y no se cambió el margen, ni el costo ni el precio del producto se modifican.

---

## Endpoints backend

| Método | Ruta | Descripción |
|---|---|---|
| `POST` | `/CompraProveedor` | Registrar una compra |
| `GET` | `/CompraProveedor/paged` | Listado paginado de compras |
| `GET` | `/CompraProveedor/{id}` | Detalle de una compra |
| `POST` | `/CompraProveedor/{id}/anular` | Anular compra (revierte stock) |
| `GET` | `/ListaPrecio/proveedor/{id}` | Listas de un proveedor |
| `GET` | `/ListaPrecio/{idLista}/items` | Productos de una lista |
| `POST` | `/ListaPrecio/{idLista}/items` | Agregar item a lista |
| `POST` | `/ListaPrecio/{idLista}/items/bulk` | Agregar múltiples items |
| `PUT` | `/ListaPrecio/{idLista}/items/{idProducto}` | Actualizar precio de item |
| `DELETE` | `/ListaPrecio/{idLista}/items/{idProducto}` | Eliminar item de lista |
| `POST` | `/Proveedor/{id}/actualizar-precios` | Actualización masiva de precios |
