# Prompt Claude Code: Importador Inteligente por Plantilla (Backend)



```md
Hola Claude. Actúa como un Arquitecto de Software Senior y experto Backend en ASP.NET Core 8.
Debemos implementar un "Caso de Uso Rey": **La Carga de Listas de Precios por Proveedor a través de una Plantilla Excel Estandarizada.**

**EL FLUJO DE NEGOCIO Y LAUX ESPERADA:**
1. Los proveedores envían PDFs o Excels caóticos.
2. Nuestro sistema no los va a parsear directamente. El usuario primero **descargará una Plantilla Excel** de nuestro sistema para el `Proveedor X`.
3. Esa plantilla contendrá los productos que el sistema ya conoce, o filas en blanco. Sus columnas estrictas serán: `CodigoBarra`, `NombreProducto`, `NuevoCosto`, y `MargenGanancia`.
4. El operario llena la plantilla (copiando del PDF del proveedor) y sube este Excel estandarizado.
5. El sistema actualiza el Costo y Margen en la tabla `ProductoListaprecioProveedor`. Opcionalmente (por un Checkbox UI), recalcula y pisa el Precio de Venta al público en `Producto`.

**CONTEXTO ARQUITECTÓNICO:**
- Sigue las reglas de `AGENTS.md` (Result Pattern, AutoMapper, capas estrictas).
- Modelo `ProductoListaprecioProveedor`: columnas `IdLista`, `IdProducto`, `Precio` (Costo), `Margen`.
- Modelo `Producto`: columna `Precio` (Precio de Venta final).
- Recuerda que un Producto se compra a MÚLTIPLES proveedores. El costo es de la relación, el precio de venta es único del producto.

**ESTRATEGIA DE EJECUCIÓN:**
Orquesta la implementación revisando los archivos relevantes y ejecuta el código en un solo bloque maestro. Asegúrate de compilar al final (`dotnet build`).

### ETAPA ÚNICA: Endpoints de Plantilla e Importación

1. **Endpoint 1: Descargar Plantilla (`GET /ProductoListaPrecioProveedor/{idLista}/plantilla-excel`)**
   - Usa `EPPlus` (o similar) para generar un Excel en memoria.
   - Headers: `CodigoBarra`, `NombreProducto`, `NuevoCosto`, `MargenGanancia`.
   - Llenado (opcional/ideal): Precarga la hoja con los productos asociables o ya asociados a esa `IdLista`/`IdProveedor` para facilitar la vida al usuario de la ferretería. Los campos `Costo` y `Margen` pueden venir vacíos o con el último valor.
   - Retorna un `FileContentResult` con el MIME de Excel.

2. **Endpoint 2: Importar Plantilla (`POST /ProductoListaPrecioProveedor/{idLista}/importar`)**
   - Recibe `IFormFile Archivo` y `[FromForm] bool ActualizarPrecioVenta`.
   - Valida extensión (solo .xlsx o .csv).
   - Servicio lector: iterar las filas del Excel subido.
   - Búsqueda: Por cada fila, cruzar `CodigoBarra` con la DB (`Producto`). **Ignorar si no existe** (No se deben crear productos automáticamente a través de este proceso).
   - **Upsert en Tabla Puente**:
     - Buscar si ya existe la relación `IdLista` - `IdProducto` en `ProductoListaprecioProveedor`.
     - Si existe, pisar `Precio` (con `NuevoCosto`) y pisar `Margen` (con `MargenGanancia` si viene).
     - Si NO existe, insertarlo.
   - **Recálculo de Venta**: 
     - Si `ActualizarPrecioVenta` == TRUE, toma ese `NuevoCosto` y el `Margen` de la fila.
     - `Producto.Precio = NuevoCosto * (1 + (Margen / 100))`.
     - Guardar cambios.

3. **Manejo de Errores y Transacciones**:
   - Envuelve el proceso de actualización en un `BeginTransactionAsync`.
   - Retorna un DTO detallado (`ImportResultDTO`) que incluya: Total procesados, Total actualizados, y **Lista de Detalles/Códigos de barras Ignorados (no encontrados)**. Esto es fundamental para que el frontend pueda avisarle al operario cuáles artículos debe dar de alta primero manualmente.

**Instrucción Final**:
Analiza los controladores y servicios actuales. Implementa los DTOs faltantes, los dos endpoints en el controlador que corresponda y la lógica de EPPlus en el servicio. Tu código debe ser defensivo y manejar Nulos en las celdas del Excel. ¡Despliega tus herramientas y crea esta funcionalidad vital!
```
