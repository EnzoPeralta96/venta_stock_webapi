# Proyecto Venta Stock Backend - Exportaciones PDF y Excel (Ventas y Cuentas Corrientes)

Hola Claude. Como el equipo hizo un trabajo magistral usando `QuestPDF` y `EPPlus` para exportar el registro de "Compras a Proveedores", ahora necesitamos escalar esa misma arquitectura perfecta hacia los dos módulos cardinales del ERP comercial: **Ventas (`Features/Sale`)** y **Cuenta Corriente (`Features/CurrentAccount`)**.

## Contexto y Arquitectura Global
Las directivas arquitectónicas son sagradas: 
1. Los controladores deben permanecer delgados (`return File(result.Value, "application/pdf")`).
2. La carga pesada y configuración de EPPlus/QuestPDF se desplaza a la carpeta `/PDF` y dentro de los métodos del respectivo Service (devolviendo `Result<byte[]>`).
3. El enfoque es puramente modular (6 endpoints nuevos en total).

---

## Especifiación 1: Módulo de Ventas (`Features/Sale`)

Queremos replicar el modelo del Proveedor, pero para el Cliente. Debes inyectar 4 endpoints nuevos en el `SaleController` y en el `ISaleService`:

### Endpoints (Filtros: `DateOnly? fechaDesde, DateOnly? fechaHasta, string estadoVenta`)
1. **Listado General:** `GET /export/excel` y `GET /export/pdf`. (Carga todas las ventas filtradas del sistema).
2. **Historial de Cliente:** `GET /cliente/{idCliente}/export/excel` y `GET /cliente/{idCliente}/export/pdf`.

### Endpoint del Comprobante (Filtro: `int idVenta`)
3. **Ticket Individual:** `GET /{idVenta}/export/excel` y `GET /{idVenta}/export/pdf`.

> **[REQUISITO CRÍTICO DE UX PARA EL COMPROBANTE DE VENTA]**
> Para el Ticket Individual, al recorrer el detalle de los productos (`DetalleVentum`), debes mostrar explícitamente el sufijo de la **Unidad de Medida** y renderizar las cantidades como decimales.
>
> Formato a buscar en la celda: `Cantidad (decimal) [UnidadMedida.Abreviatura] x Precio Unitario`.
> Ejemplo real que queremos ver impreso: *"Clavos Punta París  |  1.50 [kg]  |  $200.00 x kg  |  $300.00"*. (Incluye la relación `.Include(x => x.IdUnidadMedidaNavigation)` en tu query LINQ).

---

## Especificación 2: Módulo de Cuenta Corriente (`CurrentAccount`)

A diferencia de una simple tabla de ventas, este módulo se va a exportar como el clásico **"Estado de Cuenta Bancario"**.

### Endpoints (Filtros: `DateOnly? fechaDesde, DateOnly? fechaHasta, int? idTipoMovimientoCc`)
1. **Reporte Resumen del Cliente:** `GET /cliente/{idCliente}/export/excel` y `GET /cliente/{idCliente}/export/pdf`.

### Formato y DTO Requerido para el "Estado de Cuenta"
El cuadro que dibujarás con `QuestPDF` para la Cuenta Corriente del cliente moroso debe ser idéntico al que entrega el banco, utilizando la misma lógica temporal:

| Fecha | Tipo | Comprobante / Detalle | Debe | Haber | Saldo |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 10/01 | FACTURA (Venta) | Venta N° 001-4992 | $ 10,000.00 | - | **$ 10,000.00** |
| 15/01 | RECIBO (Pago) | A cuenta por transfe. | - | $ 5,000.00 | **$ 5,000.00** |

**Reglas de Mapeo Interno:**
- **Debe (Cargos):** Es la plata que asfixia al cliente. Si el MovimientoCc representa Deuda nueva (Factura, Nota de Débito), pon su importe en la columna Debe.
- **Haber (Abonos):** Si es plata que entra a la empresa (Recibo de Pago, Nota de Crédito), el importe va al Haber.
- **Saldo (Balance):** NO hagas cálculos matemáticos en memoria en QuestPDF. Simplemente dibuja el número que ya está precalculado en la columna **`SaldoActual`** de la tabla `MovimientoCc` para cada iteración de fila.

### Criterio de Finalización
Delegga tus agentes y clona/reutiliza fuertemente el esqueleto que tienes en `Features/CompraProveedor/PDF/CompraProveedorReportDocument.cs`. Modifica textos, suma los logos si están, y cuando termines el compendio de código compila el backend para evitar errores de sintaxis o null exceptions.
