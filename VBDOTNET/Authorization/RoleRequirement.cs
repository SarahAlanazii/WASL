using Microsoft.AspNetCore.Authorization;

namespace Wasl.Authorization
{
    /// <summary>
    /// Custom authorization requirement for role checking
    /// Equivalent to CheckRole middleware in Laravel
    /// </summary>
    public class RoleRequirement : IAuthorizationRequirement
    {
        public string[] Roles { get; }

        public RoleRequirement(params string[] roles)
        {
            Roles = roles;
        }
    }
}
