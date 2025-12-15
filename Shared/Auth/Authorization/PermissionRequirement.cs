using Microsoft.AspNetCore.Authorization;

namespace venta_stock_webapi.Shared.Auth.Authorization
{
    public class PermissionRequirement : IAuthorizationRequirement
    {
        public string Permission { get; }
        public PermissionRequirement(string permission)
        {
            Permission = permission;
        }
    }
}