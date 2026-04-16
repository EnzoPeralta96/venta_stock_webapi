# Spec: Movimientos de Stock — Observaciones de Presentación

## Contexto

Durante la presentación del proyecto se realizaron dos observaciones sobre el módulo de stock:

1. **Alta de producto no registra movimiento**: Al crear un producto con stock inicial, ese stock no queda registrado en el ledger de movimientos (`movimiento_stock`).
2. **Tabla Kardex incompleta**: La tabla solo muestra `Cantidad` y `StockResultante`. Debe mostrar tres columnas: `StockAnterior`, `Cantidad`, `StockResultante`.

---

## Estado actual de cada cambio

### ✅ HECHO — Observación 2: StockAnterior en el Kardex

Ambos archivos ya fueron modificados correctamente y no requieren nada más:

**`Features/StockMovement/DTO/MovimientoStockDTO.cs`**
- Propiedad `StockAnterior` ya agregada.

**`Features/StockMovement/Profile/StockMovementProfile.cs`**
- Mapping ya agregado: `StockAnterior = src.StockResultante - src.Cantidad` (sin tocar la BD, cálculo en runtime).

---

### ✅ HECHO — Enum C# actualizado

**`Models/MovimientoStock.cs`**
- `AltaProducto = 8` ya agregado al enum `TipoMovimientoStockEnum`.

---

### ❌ PENDIENTE — Base de datos: insertar nuevo tipo de movimiento

Hay que ejecutar el siguiente INSERT en PostgreSQL (Railway).
El archivo SQL ya fue creado en `DbScript/add_tipo_movimiento_alta_producto.sql`:

```sql
INSERT INTO tipo_movimiento_stock (id_tipo_movimiento_stock, nombre, descripcion, activo, es_sistema, es_positivo)
VALUES (
    8,
    'Alta de Producto',
    'Stock inicial registrado al momento de dar de alta un producto en el sistema.',
    true,
    true,
    true
)
ON CONFLICT (id_tipo_movimiento_stock) DO NOTHING;
```

> ⚠️ Sin este INSERT, cualquier alta de producto con stock > 0 fallará por violación de FK.

---

### ❌ PENDIENTE — Observación 1: registrar movimiento en `ProductServices.Create`

**Archivo**: `Features/Product/Services/ProductServices.cs`  
**Método**: `Create(ProductDTO productDTO)` — línea ~74

**Estado actual del método** (sin el cambio):
```
await _productRepository.Create(product);
await transaction.CommitAsync();   // <-- acá debe ir el bloque
await LogAsync(...);
return Result<bool>.Success();
```

**Qué hay que agregar** (entre `Create` y `CommitAsync`):

```csharp
// Si hay stock inicial > 0, registrarlo como movimiento de tipo AltaProducto.
// Si el stock inicial es 0, no se registra movimiento (el Kardex arranca vacío
// y el primer movimiento real mostrará StockAnterior = 0 correctamente).
// La transacción ya fue abierta con BeginTransactionAsync() más arriba,
// y el interceptor existente en Data/ ya hizo el set_config de app.user_id.
// RegistrarMovimientoAsync detecta la transacción activa y participa en ella (no abre una nueva).
if (productDTO.Stock > 0)
{
    var movResult = await _stockMovementService.RegistrarMovimientoAsync(
        product.IdProducto,
        TipoMovimientoStockEnum.AltaProducto,
        productDTO.Stock,
        "STOCK INICIAL AL DAR DE ALTA EL PRODUCTO",
        _userContext.UserId);

    if (!movResult.IsSuccess)
    {
        await transaction.RollbackAsync();
        return Result<bool>.Failure(ProductErrorCode.error_inesperado);
    }
}
```

**Notas importantes para Claude Code**:
- `_stockMovementService` ya está inyectado en `ProductServices` (ver constructor).
- `_userContext.UserId` ya está disponible (mismo patrón que usa `LogAsync`).
- **NO** agregar `ExecuteSqlRawAsync("SELECT set_config(...)")` — el interceptor en `Data/` ya lo hace al abrir la transacción con `BeginTransactionAsync()`.
- El bloque va **antes** de `CommitAsync`, **dentro** del `try`.
- Si el movimiento falla → rollback + retornar `error_inesperado`. Esto asegura que nunca quede un producto en BD sin su movimiento de alta registrado.

---

### ❌ PENDIENTE — Stock obligatorio en `ProductDTO`

**Archivo**: `Features/Product/DTO/ProductDTO.cs`

**Regla de negocio**: El stock es siempre requerido y debe ser `>= 0`. No puede ser `null`.

**Cambio en `ProductDTO`**:
```csharp
// ANTES
public decimal? Stock { get; set; }

// DESPUÉS
[Required]
[Range(0, double.MaxValue, ErrorMessage = "El stock debe ser mayor o igual a 0.")]
public decimal Stock { get; set; }
```

> Agregar el `using System.ComponentModel.DataAnnotations;` si no está presente.

**Impacto del cambio**:
- `ProductServices.Create()`: la condición ya usa `productDTO.Stock > 0` (no nullable → sin `.HasValue`).
- `ProductServices.Update()`: revisar si hace falta ajustar alguna referencia a `Stock` — probablemente no, ya que Update no toca el stock directamente.
- `ProductDetailDTO`: el campo `Stock` en este DTO es de lectura (viene de la entidad), puede mantenerse como `decimal?` si la entidad lo es, o cambiarse a `decimal` si se prefiere consistencia. **Decisión: dejarlo como está en `ProductDetailDTO`**, solo cambiar el DTO de input (`ProductDTO`).

---

## Resumen de archivos a tocar por Claude Code

| Archivo | Acción |
|---|---|
| `Features/Product/Services/ProductServices.cs` | Agregar bloque de movimiento en `Create()` |
| `Features/Product/DTO/ProductDTO.cs` | `Stock` pasa de `decimal?` a `decimal` con validaciones `[Required, Range]` |
| Base de datos (Railway) | Ejecutar `DbScript/add_tipo_movimiento_alta_producto.sql` |

## Archivos ya completados (no modificar)

| Archivo | Cambio aplicado |
|---|---|
| `Models/MovimientoStock.cs` | `AltaProducto = 8` en el enum |
| `Features/StockMovement/DTO/MovimientoStockDTO.cs` | Propiedad `StockAnterior` |
| `Features/StockMovement/Profile/StockMovementProfile.cs` | Mapping `StockAnterior = StockResultante - Cantidad` |
