using Microsoft.AspNetCore.Authorization;

namespace JobTracker.Api.Auth
{
    public class RoleRequirement : IAuthorizationRequirement
    {
        public string Role { get; }

        public RoleRequirement(string role)
        {
            Role = role;
        }
    }

    public class RoleHandler : AuthorizationHandler<RoleRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
        {
            var user = context.User;

            if (!user.Identity?.IsAuthenticated ?? true)
            {
                return Task.CompletedTask;
            }

            // Check if user has the required role claim
            if (user.HasClaim(c => c.Type == "role" && c.Value == requirement.Role))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }

    public class HasRoleAttribute : AuthorizeAttribute
    {
        public HasRoleAttribute(string role) : base(policy: $"HasRole_{role}")
        {
        }
    }
}