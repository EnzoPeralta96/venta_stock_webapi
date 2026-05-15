using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using venta_stock_webapi.Shared.Identity;

namespace venta_stock_webapi.Data
{
    public class AuditSessionInterceptor : SaveChangesInterceptor
    {
        private readonly IUserContext _userContext;

        public AuditSessionInterceptor(IUserContext userContext)
        {
            _userContext = userContext;
        }

        private void SetAuditContext(DbContext? context)
        {
            if (context is null) return;

            var userId = _userContext.UserId;
            var userName = _userContext.UserName;
            
            context.Database.ExecuteSqlRaw(
                "SELECT set_config('app.user_id', {0}::text, true);", userId
            );

            context.Database.ExecuteSqlRaw(
                "SELECT set_config('app.username', {0}, true);",userName?? ""
            );
        }

         private async Task SetAuditContextAsync(DbContext? context, CancellationToken ct)
        {
            if (context is null) return;

            var userId = _userContext.UserId;
            var userName = _userContext.UserName;
            
            await context.Database.ExecuteSqlRawAsync(
                "SELECT set_config('app.user_id', {0}::text, true);", userId
            );

            await context.Database.ExecuteSqlRawAsync(
                "SELECT set_config('app.username', {0}, true);",userName?? ""
            );
        }

        public override InterceptionResult<int> SavingChanges(
            DbContextEventData eventData,
            InterceptionResult<int> result)
        {
           SetAuditContext(eventData.Context);
           return base.SavingChanges(eventData, result);
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default
        )
        {
            await SetAuditContextAsync(eventData.Context, cancellationToken);
            return await base.SavingChangesAsync(eventData, result, cancellationToken);
        }

    }
}