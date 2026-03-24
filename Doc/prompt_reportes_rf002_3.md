# Proyecto Venta Stock WebAPI - Sprint de Reportes (RF002.3)

Hola Claude. Somos un equipo de desarrollo construyendo el backend de un sistema de gestión de stock y ventas para el proyecto final de la universidad de la carrera "Programador Universitario". El backend está construido en C# ASP.NET Core 8 con EF Core (Database-First modificado con migraciones) y PostgreSQL.

Actualmente estamos trabajando en el requerimiento **RF002.3** que consiste en agregar métricas y reportes financieros. Mi compañero ya adelantó la base del módulo `Report` con algunos endpoints básicos, pero nos juntamos con el Arquitecto de Software para diseñar e investigar cómo resolver los reportes más complejos que faltan en base a nuestro modelo de dominio. 

Llegamos a dos conclusiones importantes sobre nuestra base de datos que debes conocer para esta tarea:
1. **Margen de Utilidad:** No guardamos el costo histórico directamente en la tabla de ventas. Para saber cuánto nos costó un producto vendido, tenemos que ir a buscar su última entrada en la tabla `CompraProveedorDetalle` (la cual registra las compras a proveedores con sus precios unitarios).
2. **Cuenta Corriente y Tiempos de Cobro:** Tenemos un ledger muy robusto en la tabla `MovimientoCc` para las cuentas corrientes. Las ventas generan deuda allí, y los pagos específicos de facturas se registran bajo el `IdTipoMovimiento == 8` (`PAGO_FACTURA`), los cuales se vinculan a la venta a través del campo `IdVenta`. Esto nos permite cruzar de forma exacta cuándo se generó una factura y cuándo se terminó de pagar.

Te pido que tomes este contexto y el plan de implementación detallado a continuación para escribir el código faltante.

---

# Contexto y Tarea Técnica
Eres un experto desarrollador C# .NET 8. Necesito que implementes los endpoints de reportes faltantes en el módulo `Report`. 

La arquitectura estricta del proyecto exige el uso de: Controllers limpios, capa de Services, Repository pattern, DTOs estrictos (AutoMapper) y nuestro patrón `Result<T>` con `MessageProvider` para manejo de errores. Usa `.AsNoTracking()` en todas las consultas de lectura.

## Metodología de Trabajo Exigida
Necesito que trabajes estrictamente de la siguiente manera:
1. **Analiza** cuidadosamente estas indicaciones y el código base existente que se menciona.
2. **Crea un plan de ejecución estructurado y delegalo usando agentes** para las distintas partes del código (ej: un agente para DTOs, uno para Repositories, otro para Services/Controllers).
3. **Espera a que cada agente reporte** que ha terminado su subtarea.
4. **Revisa el código final** escrito por los agentes verificando que se adhiere a la arquitectura del proyecto y compila sin errores.
5. **Cero invenciones:** Si tienes dudas sobre alguna regla de negocio, cómo calcular algo específico, o falta algún archivo, **pregúntame directamente** en vez de asumir o inventar una solución.

## Estado del Módulo
El módulo `Report` ya existe en `Features/Report/`. Ya tiene implementado:
- Total vendido en período
- Ventas por período (día/semana/mes)
- Artículo más vendido
- Productos/Categorías más vendidas (Top N)

## Lo que debes implementar ahora

### 1. Nuevos DTOs a crear en `Features/Report/DTO/`:
- **`MargenUtilidadDTO.cs`**: `TotalVentas` (decimal), `TotalCostos` (decimal), `UtilidadBruta` (decimal), `MargenPorcentaje` (decimal).
- **`ClienteFrecuenteDTO.cs`**: `IdCliente` (int), `NombreCliente` (string), `CantidadVentas` (int), `TotalComprado` (decimal).
- **`TiempoCobroDTO.cs`**: `PromedioDiasCobro` (double).

### 2. Modificación de `IReportRepository` y `ReportRepository`:
Agrega las siguientes consultas a la interfaz y su implementación:

**A. Margen de Utilidad Bruta `Task<MargenUtilidadDTO> GetMargenUtilidadAsync(DateTime fechaDesde, DateTime fechaHasta)`**
- Regla de negocio: El precio de venta está en `DetalleVenta` (`SubTotal`). El COSTO histórico del producto NO está en `DetalleVenta`. 
- Para calcular el costo de la venta, debes hacer un subquery a la tabla `CompraProveedorDetalle`, buscando el `PrecioUnitario` (costo) de *la última compra de ese producto* que se haya realizado en una fecha anterior o igual a la fecha de la venta. Si el producto nunca tuvo compras, asume costo 0. La fórmula requerida es `(TotalVentas - TotalCostos)`. Multiplica el costo unitario encontrado por la cantidad vendida.

**B. Clientes más frecuentes `Task<List<ClienteFrecuenteDTO>> GetClientesMasFrecuentesAsync(DateTime fechaDesde, DateTime fechaHasta, int topN)`**
- Agrupar la entidad `Venta` (estado = 2) filtrando por fechas, agrupado por `IdCliente`, ordenar por `Count()` de ventas descendente y tomar el `topN`. Obtén también el join con Cliente para obtener el Nombre o Razón Social.

**C. Tiempo Promedio de Cobro `Task<TiempoCobroDTO> GetTiempoPromedioCobroAsync(DateTime fechaDesde, DateTime fechaHasta)`**
- Regla de negocio: Cruza la tabla `Venta` con `MovimientoCc` a través del campo `IdVenta`, donde `IdTipoMovimiento == 8` y `EsAnulado == false`.
- Debes obtener la diferencia en días entre la `Venta.Fecha` y la `MovimientoCc.Fecha` (del pago). Luego realizar un Average (promedio) de esos días.

**D. Monto Total Adeudado General `Task<decimal> GetMontoTotalAdeudadoAsync()`**
- Buscar el ÚLTIMO movimiento cronológico en `MovimientoCc` DENTRO de cada cuenta cliente `IdCliente`. Sumar el campo `SaldoActual` de todos esos últimos movimientos siempre que `SaldoActual > 0`.

**E. Ajuste a 'Productos más vendidos'**
- Modifica `GetProductosMasVendidosAsync` en la interfaz y repositorio para recibir un parámetro extra opcional: `int? idCategoria = null`. 
- Si `idCategoria` tiene valor, filtra el query inicial para que solo obtenga los productos de esa categoría.

### 3. Servicios y Controllers
- Agrega los métodos correspondientes en `IReportService` y `ReportService` que llamen al repositorio, apliquen validaciones lógicas sobre las fechas (como ya se hace en otros métodos), capturen excepciones, y devuelvan `Result<T>`.
- Registra en `_auditRepository.LogAsync` cada vez que se ejecute la consulta (mirar cómo está hecho en el resto del servicio).
- Crea los 4 nuevos endpoints en `ReportController` (`[HttpGet]`, protegidos con `[Authorize(Policy = "PERM:REP_GENERATE")]`):
  - `GET /api/Report/margen-utilidad`
  - `GET /api/Report/clientes-frecuentes`
  - `GET /api/Report/tiempo-promedio-cobro`
  - `GET /api/Report/deuda-total`
- Ajusta el endpoint existente `GET /api/Report/productos-mas-vendidos` para recibir `[FromQuery] int? idCategoria`.

## Instrucciones Finales
Recuerda: Planifica con agentes, espera sus reportes, revisa el código final, y pregúntame ante cualquier mínima duda. NO rompas el patrón de `Result<T>` y `MessageProvider`.
