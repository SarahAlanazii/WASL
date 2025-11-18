using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Wasl.Authorization
{
    /// <summary>
    /// Authorization handler for role requirement
    /// </summary>
    public class RoleAuthorizationHandler : AuthorizationHandler<RoleRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RoleRequirement requirement)
        {
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                context.Fail();
                return Task.CompletedTask;
            }

            var userRole = context.User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userRole))
            {
                context.Fail();
                return Task.CompletedTask;
            }

            if (requirement.Roles.Contains(userRole))
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
            }

            return Task.CompletedTask;
        }
    }
}
