
using Microsoft.AspNetCore.Authorization;

namespace venta_stock_webapi.Shared.Auth.Authorization;

public class PermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userIsNotLogged = context.User?.Identity?.IsAuthenticated != true;
        if (userIsNotLogged) return Task.CompletedTask;

        var  hasPermissionClaim = context.User.Claims.Any(c => c.Type == AuthConstants.PermissionClaimType && string.Equals(c.Value, requirement.Permission, StringComparison.OrdinalIgnoreCase));

        if (hasPermissionClaim) context.Succeed(requirement);
        
        return Task.CompletedTask;
    }
}
