using Microsoft.AspNetCore.Authorization;
using System.Data;
using Wasl.Constants;

namespace Wasl.Authorization
{
    /// <summary>
    /// Custom authorization attribute for easier usage
    /// </summary>
    public class RoleAuthorizeAttribute : AuthorizeAttribute
    {
        public RoleAuthorizeAttribute(params string[] roles)
        {
            Roles = string.Join(",", roles);
        }
    }

    /// <summary>
    /// Extension methods for authorization configuration
    /// ADD THIS FILE - It's missing from your code!
    /// </summary>
    public static class AuthorizationPolicyExtensions
    {
        public static void AddWaslAuthorizationPolicies(this AuthorizationOptions options)
        {
            // Admin only policy
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole(DBConstants.ROLE_ADMIN));

            // Company only policy
            options.AddPolicy("CompanyOnly", policy =>
                policy.RequireRole(DBConstants.ROLE_COMPANY));

            // Provider only policy
            options.AddPolicy("ProviderOnly", policy =>
                policy.RequireRole(DBConstants.ROLE_PROVIDER));

            // Company or Provider policy
            options.AddPolicy("CompanyOrProvider", policy =>
                policy.RequireRole(DBConstants.ROLE_COMPANY, DBConstants.ROLE_PROVIDER));

            // Admin or Company policy
            options.AddPolicy("AdminOrCompany", policy =>
                policy.RequireRole(DBConstants.ROLE_ADMIN, DBConstants.ROLE_COMPANY));

            // Admin or Provider policy
            options.AddPolicy("AdminOrProvider", policy =>
                policy.RequireRole(DBConstants.ROLE_ADMIN, DBConstants.ROLE_PROVIDER));

            // Any authenticated user
            options.AddPolicy("Authenticated", policy =>
                policy.RequireAuthenticatedUser());
        }
    }
}
