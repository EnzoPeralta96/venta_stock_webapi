using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using proyecto_venta_stock.Data;
using venta_stock_webapi.Shared.Identity;

namespace venta_stock_webapi.Data.Audit
{
    public class AuditDbSession
    {
        public static async Task SetAsync(VentaStockContext dbContext, IUserContext userContext, CancellationToken ct = default)
        {
            if (!userContext.IsAuthenticated || userContext.UserId <= 0)
                throw new InvalidOperationException("Auditoría: falta usuario autenticado para setear app.user_id.");

            await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT set_config('app.user_id', {0}::text, true);",
                userContext.UserId);

            await dbContext.Database.ExecuteSqlRawAsync(
                "SELECT set_config('app.username', {0}, true);",
                userContext.UserName ?? "");
        }

    }
}