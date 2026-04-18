# Explicación: Auditoría — Interceptor vs ExecuteUpdateAsync

## Contexto

El sistema de auditoría funciona mediante **triggers en PostgreSQL**. Cada trigger llama a `fn_auditoria_generica()`, que lee la variable de sesión `app.user_id` para saber qué usuario realizó la acción.

El backend es responsable de setear esa variable antes de cada operación DML (INSERT/UPDATE/DELETE).

---

## Cómo se setea `app.user_id`

El backend usa `AuditSessionInterceptor` — un `SaveChangesInterceptor` de EF Core registrado globalmente en `Program.cs`. Se ejecuta automáticamente **justo antes de cada `SaveChangesAsync()`**:

```csharp
public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(...)
{
    await context.Database.ExecuteSqlRawAsync(
        "SELECT set_config('app.user_id', {0}::text, true);", userId);
    await context.Database.ExecuteSqlRawAsync(
        "SELECT set_config('app.username', {0}, true);", userName);
    ...
}
```

El `true` en `set_config(..., true)` es el parámetro `is_local`. Significa que el valor **solo vive dentro de la transacción activa**. Cuando la transacción termina, se resetea a `""`.

---

## El problema con `ExecuteUpdateAsync`

`ExecuteUpdateAsync` ejecuta un `UPDATE` SQL **directamente contra la DB**, sin pasar por el pipeline de EF Core. Por lo tanto:

```
ExecuteUpdateAsync()
  → UPDATE directo en la DB   ← el interceptor NUNCA se ejecuta
  → Trigger lee app.user_id  → encuentra ""
  → PostgreSQL intenta castear "" a integer → ERROR:
    "invalid input syntax for type integer: """
```

El flujo correcto (con `SaveChangesAsync`) sería:

```
SaveChangesAsync()
  → AuditSessionInterceptor.SavingChangesAsync()
      → set_config('app.user_id', userId, true)   ✓
  → INSERT/UPDATE en la DB
  → Trigger lee app.user_id → encuentra el valor  ✓
```

---

## La solución

Cuando el repositorio usa `ExecuteUpdateAsync` (o `ExecuteSqlRawAsync`), hay que **llamar `set_config` manualmente** en el servicio, dentro de la transacción abierta, antes de la operación:

```csharp
private async Task SetAuditContextAsync()
{
    await _context.Database.ExecuteSqlRawAsync(
        "SELECT set_config('app.user_id', {0}::text, true);", _userContext.UserId);
    await _context.Database.ExecuteSqlRawAsync(
        "SELECT set_config('app.username', {0}, true);", _userContext.UserName ?? "");
}

public async Task<Result<string>> UpdateAccountConfig(UpdateAccountConfigDTO dto)
{
    using var transaction = await _context.Database.BeginTransactionAsync();
    try
    {
        // ... validaciones ...

        await SetAuditContextAsync();                          // ← setear ANTES
        await _accountConfigRepository.UpdateAccountConfigAsync(config); // ExecuteUpdateAsync
        // Trigger dispara → lee app.user_id → encuentra el valor ✓

        await transaction.CommitAsync();
        return Result<string>.Success();
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();
        ...
    }
}
```

El `set_config` con `is_local=true` persiste durante toda la transacción. El trigger ejecuta dentro de esa misma transacción → lee el valor correctamente.

---

## Regla general

| Operación en el repositorio | Interceptor dispara? | Qué hacer en el servicio |
|---|---|---|
| `SaveChangesAsync()` | ✅ Sí — automático | Nada extra |
| `ExecuteUpdateAsync()` | ❌ No | Llamar `SetAuditContextAsync()` antes |
| `ExecuteSqlRawAsync()` | ❌ No | Llamar `SetAuditContextAsync()` antes |

---

## Requisitos para que funcione

1. **Transacción abierta**: `set_config(..., is_local=true)` solo persiste dentro de una transacción. Sin `BeginTransactionAsync()` el valor se descarta antes del DML.
2. **`IUserContext` inyectado**: el servicio necesita `IUserContext` para obtener `UserId` y `UserName`.
3. **Llamar antes del DML**: `SetAuditContextAsync()` debe ir antes de la operación que dispara el trigger.
