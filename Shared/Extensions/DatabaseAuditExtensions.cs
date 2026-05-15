using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using venta_stock_webapi.Shared.Identity;

namespace venta_stock_webapi.Shared.Extensions;

public static class DatabaseAuditExtensions
{
    /// <summary>
    /// Setea las variables de sesión de PostgreSQL requeridas por los triggers de auditoría.
    /// Debe llamarse dentro de una transacción abierta, antes de cualquier ExecuteUpdateAsync
    /// o ExecuteSqlRawAsync que dispare un trigger (ya que esas operaciones bypasean el interceptor).
    /// </summary>
    public static async Task SetAuditContextAsync(this DatabaseFacade db, IUserContext userContext)
    {
        await db.ExecuteSqlRawAsync(
            "SELECT set_config('app.user_id', {0}::text, true);", userContext.UserId);
        await db.ExecuteSqlRawAsync(
            "SELECT set_config('app.username', {0}, true);", userContext.UserName ?? "");
    }

    /// <summary>
    /// Overload para cuando el userId viene como parámetro explícito (no desde IUserContext).
    /// </summary>
    public static async Task SetAuditContextAsync(this DatabaseFacade db, int userId)
    {
        await db.ExecuteSqlRawAsync(
            "SELECT set_config('app.user_id', {0}::text, true);", userId);
    }
}
