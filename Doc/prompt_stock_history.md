# Proyecto Venta Stock - Fase 2: Implementación de Historial de Stock (Backend)

Hola Claude. Como ya terminamos la integración del "Ajuste Manual de Stock" (Fase 1), ahora pasaremos a la **Fase 2: El Historial de Movimientos de Stock (Kardex)**.
Necesitamos exponer la información del Ledger de Stock (`MovimientoStock`) hacia el frontend para que el cliente pueda ver la trazabilidad de cada producto.

## Metodología y Patrones Arquitectónicos
Debes programar el backend siguiendo **ESTRICTAMENTE** el patrón de diseño que usamos en `Features/User` y `Features/Client`. Estudia esos módulos si tienes dudas. 
Las reglas inquebrantables son:
1. **Controllers Thin (Delgados):** No debe haber lógica de negocio, try-catch ni acceso a DB en el Controller. Solo llamada al Service, chequeo de `!result.IsSuccess` y resolución del mensaje con `MessageProvider`.
2. **Uso de `Result<T>`:** Todo Service retorna `Task<Result<T>>`. Nunca lanzar excepciones hacia el Controller.
3. **Manejo de Errores con `MessageProvider`:** Usa tu enum `StockMovementErrorCode` (ej: `unexpected_error`, `product_not_found`) y resuelve los mensajes amigables como en `UserController.cs` (línea 49-52 aprox).
4. **Paginación Genérica:** Utilizar nuestra clase `PagedList<T>.CreateAsync()`.
5. **No usar Eager Loading innecesario en memoria:** En las queries que se van a paginar con AutoMapper (`ProjectTo`), no hagas `.ToList()` antes de paginar. Pasa el `IQueryable` directo al paginador.
6. Usa **Agentes** para leer código de referencia, planificar e implementar.

---

## Especificación Técnica a Implementar

Todo esto debe ir dentro del feature `Features/StockMovement/`.

### 1. DTOs de Salida (`Features/StockMovement/DTO/`)
Crea los DTOs que enviaremos al Front. No uses las entidades EF directamente:

**`TipoMovimientoStockDTO.cs`**:
```csharp
public class TipoMovimientoStockDTO
{
    public int IdTipoMovimientoStock { get; set; }
    public string Nombre { get; set; } = null!;
}
```

**`MovimientoStockDTO.cs`**:
```csharp
public class MovimientoStockDTO
{
    public int IdMovimientoStock { get; set; }
    public int IdProducto { get; set; }
    public int IdTipoMovimientoStock { get; set; }
    public string TipoMovimiento { get; set; } = null!; // Nombre del tipo mapeado
    public int Cantidad { get; set; }
    public int StockResultante { get; set; }
    public DateTime Fecha { get; set; }
    public string? Usuario { get; set; } // Nombre del Usuario (usuario.Nombre), si existe
    public string? Referencia { get; set; }
}
```

*Nota: Asegúrate de configurar en tu Profile de AutoMapper (`StockMovementProfile`) las proyecciones correctas desde `MovimientoStock` hacia `MovimientoStockDTO`.*

### 2. Capa Repository (`IStockMovementRepository` / `StockMovementRepository`)
Agrega estos métodos de lectura:

A. **`Task<List<TipoMovimientoStock>> GetTiposAsync()`**
Retorna la lista completa de tipos de movimiento para el combobox del frontend. 

B. **`IQueryable<MovimientoStock> MovementsQueryable(int idProducto, int? idTipoMovimiento)`**
Fíjate que retorna `IQueryable`. La lógica debe ser:
```csharp
var query = _dbContext.MovimientoStocks
    .AsNoTracking()
    .Where(m => m.IdProducto == idProducto);

if (idTipoMovimiento.HasValue)
    query = query.Where(m => m.IdTipoMovimientoStock == idTipoMovimiento.Value);

// IMPORTANTE: Orden descendente por defecto para que el Kardex muestre lo más reciente primero
return query.OrderByDescending(m => m.Fecha);
```

### 3. Capa Service (`IStockMovementService` / `StockMovementService`)
Agrega e implementa estos dos métodos:

A. **`Task<Result<List<TipoMovimientoStockDTO>>> GetTiposMovimientoAsync()`**
- Recupera tipos desde Repositorio.
- Mapea a `List<TipoMovimientoStockDTO>`.
- Maneja try/catch con `_logger.LogError`.
- Retorna `Result<...>.Success()`.

B. **`Task<Result<PagedList<MovimientoStockDTO>>> MovimientosPagedAsync(int idProducto, int pageIndex, int pageSize, int? idTipoMovimiento)`**
Imita la lógica de `UserService.UsersPagedAsync`:
```csharp
try
{
    // Validar si el producto existe antes de buscar movimientos
    var existeProducto = await _productRepository.Exists(idProducto);
    if (!existeProducto) return Result<PagedList<MovimientoStockDTO>>.Failure(StockMovementErrorCode.product_not_found);

    var query = _stockMovementRepository.MovementsQueryable(idProducto, idTipoMovimiento);
    var projected = _mapper.ProjectTo<MovimientoStockDTO>(query);
    var paged = await PagedList<MovimientoStockDTO>.CreateAsync(projected, pageIndex, pageSize);

    return Result<PagedList<MovimientoStockDTO>>.Success(paged);
}
catch (Exception ex)
{
    _logger.LogError("Error en paginación: " + ex);
    return Result<PagedList<MovimientoStockDTO>>.Failure(StockMovementErrorCode.unexpected_error);
}
```

### 4. Capa Controller (`StockMovementController`)
Expón los endpoints con política de lectura `PERM:PROD_READ` o la que corresponda a ver productos.

A. **`GET /api/StockMovement/tipos`**:
Llama al service de tipos y retorna Ok o NotFound.

B. **`GET /api/StockMovement/producto/{idProducto}/movimientos`**:
```csharp
[Authorize(Policy = "PERM:PROD_READ")]
[HttpGet("producto/{idProducto}/movimientos")]
public async Task<IActionResult> GetMovimientos(
    int idProducto,
    [FromQuery] int pageIndex = 1,
    [FromQuery] int? idTipoMovimiento = null)
{
    int pageSize = 10;
    var result = await _stockMovementService.MovimientosPagedAsync(idProducto, pageIndex, pageSize, idTipoMovimiento);

    if (!result.IsSuccess)
    {
        var code = (StockMovementErrorCode)result.ErrorCode;
        var errorMessage = MessageProvider.Get(StockMovementErrorDictionary.Messages, code);
        return NotFound(errorMessage); // o BadRequest según error
    }

    return Ok(result.Value); // Retorna el PagedList serializado
}
```

## Entrega
Por favor usa tus agentes para:
1. Leer los archivos en Features/User y entender los Data Shapes.
2. Escribir los DTOs, Repository, Profile, Service y Controller correspondientes.
3. Verificar que compila todo sin errores CS0118 o dependencias cíclicas y que el mapeo de AutoMapper tiene sentido, especialmente para los campos relacionales (`TipoMovimiento` y `Usuario`).
