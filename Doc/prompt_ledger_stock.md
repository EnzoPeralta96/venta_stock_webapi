# Proyecto Venta Stock - Implementación de Ledger de Stock (Movimientos de Stock)

Hola Claude. Somos un equipo de desarrollo construyendo el backend de un sistema de gestión de stock y ventas para el proyecto final de la universidad ("Programador Universitario"). El backend está en C# ASP.NET Core 8 con EF Core (Database-First modificado con migraciones) y PostgreSQL.

Nuestro profesor y arquitecto de software nos ha exigido cambiar a un modelo de "Ledger" (Libro Mayor) para el stock, dejando de mutar libremente el `Producto.Stock` y pasando a registrar cada cambio en una tabla inmutable `MovimientoStock`.

## Metodología de Trabajo Exigida
1. **Analiza** estas indicaciones.
2. **Crea un plan de ejecución estructurado y delegalo usando agentes**.
3. **Copia el código exacto** provisto en la sección "Especificación de Código" para no inventar modelos ni relaciones. Si necesitas adaptar algo menor para que compile, hazlo, pero respeta la estructura.
4. **Revisa el código final**.
5. **Cero invenciones:** Pregúntanos si tienes dudas críticas.

---

## 1. Diseño del Ledger de Stock

Debes crear exactamente estos modelos en `Models/`:

```csharp
namespace proyecto_venta_stock.Models;

public enum TipoMovimientoStockEnum
{
    IngresoCompra = 1,
    EgresoVenta = 2,
    ReingresoAnulacionVenta = 3,
    EgresoAnulacionCompra = 4,
    AjustePositivoManual = 5,
    AjusteNegativoManual = 6,
    ConsumoInternoDueno = 7
}

public class TipoMovimientoStock
{
    public int IdTipoMovimientoStock { get; set; }
    public string Nombre { get; set; } = null!;
    public string Descripcion { get; set; } = null!;
    
    public virtual ICollection<MovimientoStock> MovimientosStock { get; set; } = new List<MovimientoStock>();
}

public class MovimientoStock
{
    public int IdMovimientoStock { get; set; }
    public int IdProducto { get; set; }
    public int IdTipoMovimientoStock { get; set; }
    
    /// <summary>
    /// Cantidad del movimiento. Positiva para ingresos, negativa para egresos.
    /// </summary>
    public int Cantidad { get; set; }
    
    /// <summary>
    /// Caché del stock resultante al momento del movimiento.
    /// </summary>
    public int StockResultante { get; set; }
    
    public DateTime Fecha { get; set; }
    public int? IdUsuario { get; set; }
    public string? Referencia { get; set; }

    public virtual Producto IdProductoNavigation { get; set; } = null!;
    public virtual TipoMovimientoStock IdTipoMovimientoStockNavigation { get; set; } = null!;
    public virtual Usuario? IdUsuarioNavigation { get; set; }
}
```

Modifica `VentaStockContext.cs`:
```csharp
public virtual DbSet<TipoMovimientoStock> TipoMovimientoStocks { get; set; }
public virtual DbSet<MovimientoStock> MovimientoStocks { get; set; }

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // ... código existente ...

    modelBuilder.Entity<TipoMovimientoStock>(entity =>
    {
        entity.HasKey(e => e.IdTipoMovimientoStock);
        entity.ToTable("tipo_movimiento_stock");
        entity.Property(e => e.IdTipoMovimientoStock)
              .ValueGeneratedNever()
              .HasColumnName("id_tipo_movimiento_stock");
        entity.Property(e => e.Nombre).HasMaxLength(50).HasColumnName("nombre");
        entity.Property(e => e.Descripcion).HasMaxLength(255).HasColumnName("descripcion");
    });

    modelBuilder.Entity<MovimientoStock>(entity =>
    {
        entity.HasKey(e => e.IdMovimientoStock);
        entity.ToTable("movimiento_stock");
        entity.Property(e => e.IdMovimientoStock).HasColumnName("id_movimiento_stock");
        entity.Property(e => e.IdProducto).HasColumnName("id_producto");
        entity.Property(e => e.IdTipoMovimientoStock).HasColumnName("id_tipo_movimiento_stock");
        entity.Property(e => e.Cantidad).HasColumnName("cantidad");
        entity.Property(e => e.StockResultante).HasColumnName("stock_resultante");
        entity.Property(e => e.Fecha).HasColumnType("timestamp with time zone").HasColumnName("fecha");
        entity.Property(e => e.IdUsuario).HasColumnName("id_usuario");
        entity.Property(e => e.Referencia).HasMaxLength(150).HasColumnName("referencia");

        entity.HasOne(d => d.IdProductoNavigation)
            .WithMany() // O agrega la colección a Producto.cs si lo prefieres
            .HasForeignKey(d => d.IdProducto)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_movimientostock_producto");

        entity.HasOne(d => d.IdTipoMovimientoStockNavigation)
            .WithMany(p => p.MovimientosStock)
            .HasForeignKey(d => d.IdTipoMovimientoStock)
            .OnDelete(DeleteBehavior.Restrict)
            .HasConstraintName("fk_movimientostock_tipo");
            
        entity.HasOne(d => d.IdUsuarioNavigation)
            .WithMany()
            .HasForeignKey(d => d.IdUsuario)
            .OnDelete(DeleteBehavior.SetNull)
            .HasConstraintName("fk_movimientostock_usuario");
    });
}
```

Prepara el seeding en `VentaStockContext.cs` o en tu script inicial para cargar los 7 tipos básicos de `TipoMovimientoStockEnum`.

---

## 2. Refactor de Edición de Stock (Quitar Update Manual)
- En `Features/Product/DTO/ProductUpdateDTO.cs`, **elimina** temporal o definitivamente la propiedad `Stock` para que nadie la asigne desde el JSON.
- En `Features/Product/Services/ProductServices.cs` (método `UpdateProductoAsync`), retira o comenta la línea que asigna manualmente `existingProduct.Stock = productDTO.Stock;`.

---

## 3. Registrar Movimientos (StockMovementService)
Crea una interfaz e implementación base `IStockMovementService` para que los otros módulos la inyecten:

```csharp
public interface IStockMovementService
{
    Task<Result<bool>> RegistrarMovimientoAsync(int idProducto, TipoMovimientoStockEnum tipoMovimiento, int cantidad, string? referencia, int? idUsuario);
}
```

La implementación debe hacer:
1. Buscar el `Producto` y obtener su stock actual (ej. `30`).
2. Sumar o restar la `cantidad` en base al tipo (por ejemplo, si pasas 5 y es ingreso suma 5. Si pasas 5 y es egreso, se vuelve -5). O más fácil, exigir que `cantidad` venga ya con el signo correcto `(+5 o -2)`.
3. Guardar el nuevo `Producto.Stock`. **(La columna de caché)**
4. Crear y guardar el `MovimientoStock` con `StockResultante = Producto.Stock`.

Luego inyéctalo en:
- `CompraProveedorServices`: En `SumarStockAsync`, invocar este servicio pasando `TipoMovimientoStockEnum.IngresoCompra`.
- `SaleService` (o donde descuenten ventas): Invocar este servicio al confirmar venta con `TipoMovimientoStockEnum.EgresoVenta` y la cantidad en negativo.

---

## 4. Nuevo Módulo "Ajuste de Stock Manual"
Crea el endpoint para que un usuario pueda insertar sobrantes, faltantes o retiros.

**AjusteStockDTO.cs**:
```csharp
public class AjusteStockDTO
{
    public int IdProducto { get; set; }
    public int Cantidad { get; set; } // positivo suma, negativo resta
    public int IdTipoMovimiento { get; set; } // Que envíen el 5, 6 o 7
    public string Motivo { get; set; } = string.Empty;
}
```

**StockMovementController.cs**:
```csharp
[HttpPost("ajuste-manual")]
[Authorize(Policy = "PERM:PROD_EDIT")] // o política acorde
public async Task<IActionResult> RegistrarAjuste([FromBody] AjusteStockDTO dto)
{
    // Llama a StockMovementService validando el Enum (5,6,7)
    // Devuelve Ok o BadRequest 
}
```

## Entregables Finales
Ejecuta la implementación completa en este orden con tus agentes:
1. Crea los archivos de Models e inyéctalo en DBContext (Code-first o actualiza). Crea la migración (`dotnet ef migrations add AddStockLedger`).
2. Crea el `IStockMovementService` central.
3. Arregla UpdateProducto y engánchalo a Compras/Ventas.
4. Implementa el Controller para ajustes manuales.

---

## ✅ Implementación Realizada (2026-03-22)

### Archivos creados

| Archivo | Descripción |
|---|---|
| `Models/MovimientoStock.cs` | Enum `TipoMovimientoStockEnum` + clases `TipoMovimientoStock` y `MovimientoStock` |
| `Features/StockMovement/Services/IStockMovementService.cs` | Interfaz del servicio central de ledger |
| `Features/StockMovement/Services/StockMovementService.cs` | Implementación: carga producto con tracking, actualiza caché `Stock`, inserta `MovimientoStock`, llama `SaveChangesAsync` |
| `Features/StockMovement/DTO/AjusteStockDTO.cs` | DTO de entrada para ajustes manuales (`IdProducto`, `Cantidad`, `IdTipoMovimiento`, `Motivo`) |
| `Features/StockMovement/Messages/StockMovementErrorCode.cs` | Enum de errores + diccionario de mensajes amigables |
| `Features/StockMovement/Controllers/StockMovementController.cs` | `POST /api/StockMovement/ajuste-manual` con `[Authorize(Policy = "PERM:PROD_EDIT")]` |
| `Migrations/20260322235635_AddStockLedger.cs` | Crea `tipo_movimiento_stock`, `movimiento_stock` y siembra los 7 tipos |

### Archivos modificados

| Archivo | Cambio |
|---|---|
| `Data/VentaStockContext.cs` | Agregó `DbSet<TipoMovimientoStock>`, `DbSet<MovimientoStock>`, configuración Fluent API y seed de 7 tipos |
| `Models/MovimientoStock.cs` | Nuevo (ver arriba) |
| `Features/Product/Services/ProductServices.cs` | Eliminó `existingProduct.Stock = productDTO.Stock` — el stock ya no se asigna manualmente en Update |
| `Features/Sale/Repository/SaleRepository/ISaleRepository.cs` | Eliminó `UpdateProductStockAsync` y `RestoreProductStockAsync` |
| `Features/Sale/Repository/SaleRepository/SaleRepository.cs` | Eliminó las implementaciones de los métodos anteriores |
| `Features/Sale/Services/SaleServices/SaleService.cs` | Inyectó `IStockMovementService`; reemplazó `UpdateProductStockAsync` → `RegistrarMovimientoAsync(EgresoVenta, -cantidad, "VENTA:{codigo}")` y `RestoreProductStockAsync` → `RegistrarMovimientoAsync(ReingresoAnulacionVenta, +cantidad, "ANULACION-VENTA:{codigo}")` |
| `Features/CompraProveedor/Services/CompraProveedorServices.cs` | Inyectó `IStockMovementService`; eliminó métodos privados `SumarStockAsync`/`RestarStockAsync`; reemplazó todos sus usos por `RegistrarMovimientoAsync` con los tipos correspondientes |
| `Program.cs` | Registró `IStockMovementService → StockMovementService` como Scoped |

### Lógica de referencia por operación

| Operación | Tipo | Signo cantidad | Referencia |
|---|---|---|---|
| Crear compra | `IngresoCompra` | `+cantidad` | `COMPRA:{id}` |
| Actualizar compra (revertir anterior) | `EgresoAnulacionCompra` | `-cantidad` | `UPDATE-REVERT:COMPRA:{id}` |
| Actualizar compra (aplicar nuevos) | `IngresoCompra` | `+cantidad` | `UPDATE-APPLY:COMPRA:{id}` |
| Eliminar compra (soft delete) | `EgresoAnulacionCompra` | `-cantidad` | `DELETE:COMPRA:{id}` |
| Toggle OFF compra | `EgresoAnulacionCompra` | `-cantidad` | `TOGGLE-OFF:COMPRA:{id}` |
| Toggle ON compra | `IngresoCompra` | `+cantidad` | `TOGGLE-ON:COMPRA:{id}` |
| Crear venta | `EgresoVenta` | `-cantidad` | `VENTA:{codigoVenta}` |
| Anular venta | `ReingresoAnulacionVenta` | `+cantidad` | `ANULACION-VENTA:{codigoVenta}` |
| Ajuste manual | `5`, `6` o `7` | según lo enviado | campo `Motivo` del DTO |

---

## Integración con el Frontend

### Endpoint de ajuste manual

```
POST /api/StockMovement/ajuste-manual
Authorization: Bearer {token}  (requiere permiso PROD_EDIT)
Content-Type: application/json

{
  "idProducto": 42,
  "cantidad": 3,           // positivo = suma, negativo = resta
  "idTipoMovimiento": 5,   // 5 = AjustePositivo, 6 = AjusteNegativo, 7 = ConsumoInternoDueno
  "motivo": "Sobrante detectado en conteo físico"
}
```

**Respuestas:**
- `200 OK` → `{ "mensaje": "Ajuste de stock registrado correctamente." }`
- `400 Bad Request` → tipo de movimiento inválido o error inesperado
- `404 Not Found` → producto no encontrado

### Tipos de movimiento disponibles para el frontend

Los tipos de movimiento **manual** que el frontend puede enviar son:

| `idTipoMovimiento` | Nombre | Cuándo usarlo |
|---|---|---|
| `5` | Ajuste Positivo Manual | Sobrante en conteo físico de inventario |
| `6` | Ajuste Negativo Manual | Faltante en conteo físico de inventario |
| `7` | Consumo Interno Dueño | Retiro de mercadería para uso propio |

> Los tipos 1-4 los genera el backend automáticamente (compras y ventas). **El frontend solo puede usar 5, 6 y 7.**

### Historial de movimientos (sugerencia para futura pantalla)

El historial completo de movimientos queda en la tabla `movimiento_stock`. Si en el futuro se agrega un endpoint de consulta, el frontend puede mostrar una grilla con:
- `fecha` — cuándo ocurrió
- `idTipoMovimientoStockNavigation.nombre` — tipo legible
- `cantidad` — positivo o negativo
- `stockResultante` — stock después del movimiento
- `referencia` — qué transacción lo originó (ej. `VENTA:V-000123`)

### Stock en `Producto`

El campo `producto.stock` sigue existiendo como **caché de rendimiento**. El frontend puede seguir leyendo `stock` del endpoint de productos para mostrar el stock actual. La fuente de verdad es el ledger, pero el caché es siempre consistente (se actualiza en cada movimiento registrado).
