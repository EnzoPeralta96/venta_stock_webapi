using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace venta_stock_webapi.Shared.Auth.Authorization
{
    public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
    {
        public PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : base(options)
        {
        }

        public override Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
        {
            if(policyName.StartsWith(AuthConstants.PermissionPolicyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var permission = policyName.Substring(AuthConstants.PermissionPolicyPrefix.Length).Trim();

                if (string.IsNullOrWhiteSpace(permission)) return Task.FromResult<AuthorizationPolicy?>(null);

                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .AddRequirements(new PermissionRequirement(permission))
                    .Build();

                return Task.FromResult<AuthorizationPolicy?>(policy);
            }

            return base.GetPolicyAsync(policyName);
        }

    }
}