# Proyecto Venta Stock - Refactor Backend: Unidades de Medida y Decimales

Hola Claude. Vamos a realizar un refactor estructural muy importante **solo en el Backend**. Por tratarse de un ERP para una ferretería, necesitamos dejar de asumir que todos los productos se venden por fracciones enteras ("Unidad") y pasar a soportar **decimales** para vender por Kilo, Litro o Metro.

Esto impacta en la base de datos, en las firmas de los servicios, en los DTOs y, muy especialmente, en la lógica de Importación Masiva.

## Metodología y Patrones Arquitectónicos
Debes programar el backend siguiendo **ESTRICTAMENTE** el patrón de diseño del resto del proyecto (guíate por módulos como `Features/User` o `Features/StockMovement`):
1. **Uso de `Result<T>`:** Todo Service retorna `Task<Result<T>>`.
2. **Manejo de Errores con `MessageProvider`:** Usa Enums y diccionarios estáticos de error para devolver mensajes amigables al Controller.
3. No rompas ni omitas llamadas a la Auditoría (`LogAsync` si aplica).
4. Utiliza **agentes** para investigar las entidades afectadas y planificar tu ejecución. No improvises.

---

## Tareas a Implementar (Backend)

### FASE 1: Base de Datos y Modelos (Entity Framework)
1. **Catálogo `UnidadMedida`:**
   - Crear clase en `Models/UnidadMedida.cs` con: `IdUnidadMedida` (int, PK), `Nombre` (string, ej: "Kilogramo"), `Abreviatura` (string, ej: "kg").
2. **Actualizar `Producto`:**
   - Agregar `public int? IdUnidadMedida { get; set; }` (FK opcional, por defecto asumiremos 1=Unidad en logica).
   - Cambiar los tipos de `Stock` y `StockMinimo` de `int?` a `decimal?`.
3. **Actualizar Historial y Compras/Ventas:**
   - En `MovimientoStock.cs`: `Cantidad` y `StockResultante` pasan de `int` a `decimal`.
   - En `DetalleVentum.cs`: `Cantidad` pasa a `decimal?`.
   - En `CompraProveedorDetalle.cs`: `Cantidad` pasa a `decimal`.
4. **DbContext y Migración:**
   - Configura el Fluent API explícitamente en `VentaStockContext.cs` para que las columnas `decimal` sean creadas como `decimal(18,3)` o `numeric(18,3)` en PostgreSQL. Esto es vital para manejar "0.250 kg" pero manteniendo precisión financiera.
   - Siembra (Seed) en el DbContext: `1: Unidad (u), 2: Kilogramo (kg), 3: Metro (m), 4: Litro (l)`.
   - Genera la migración (ej: `dotnet ef migrations add SupportUnidadesDecimales`).

### FASE 2: Propagación en Servicios y DTOs
1. **Firmas de Servicios:**
   - Rastrear y actualizar TODAS las firmas de `IStockMovementService.RegistrarMovimientoAsync` para admitir `decimal cantidad`.
   - Rastrear los cambios matemáticos en `SaleService` y `CompraProveedorServices`. Si hacían `precio * cantidad`, revisa que no haya errores de casteo implícito entre `int`/`decimal` que ahora se rompan.
2. **DTOs:**
   - En `Features/Product/DTO/`: Actualizar `ProductCreateDTO`, `ProductUpdateDTO` y `ProductDTO` (cambiar stock a decimal y agregar `IdUnidadMedida`).
   - En Compras/Ventas/Ajustes: Todos los DTOs de entrada/salida (`VentaCreateDTO`, `AjusteStockDTO`, etc.) deben declarar la cantidad como `decimal`.

### FASE 3: El Parche a la Carga Masiva (Excel/CSV)
Existen deudas técnicas en el actual `POST /Product/importar` que debes parchear para que sea coherente con la nueva arquitectura del "Stock Ledger" y las unidades:
1. **Seguridad:** El endpoint está desprotegido. Agrégale el atributo `[Authorize]` con la policy adecuada de Product Create/Edit.
2. **Unidad de Medida Default:** Como el Excel no tiene columna de Unidad y para no romper compatibilidad, si la estructura EPPlus no encuentra `IdUnidadMedida`, debes inyectarle un `1` ("Unidad") a todo producto nuevo.
3. **El Match con el Ledger (¡Crítico!):** 
   - La regla técnica a partir de ahora es: *Si el import es un INSERT (producto nuevo)* y la importación decide procesar un stock inicial en el Excel (> 0), el importador NO DEBE asignar ese valor directo a `Producto.Stock`. 
   - En su lugar, debe invocar a `IStockMovementService.RegistrarMovimientoAsync(...)` con tipo `AjustePositivoManual` y el motivo `"ALTA POR CARGA MASIVA"` (o equivalente) para que el historial (Ledger) recoja el ingreso inicial correctamente.
   - Si el producto ya existía (UPDATE), el stock del excel se ignora completamente.

---

## Ejecución con Claude Code
- Despliega tus agentes paso por paso.
- Compila usando `dotnet build` frecuentemente para confirmar que tu cambio de `int` a `decimal` no rompió ningún LINQ `.Sum()` en los servicios de Reportes o Validaciones (`Features/Report/`).
