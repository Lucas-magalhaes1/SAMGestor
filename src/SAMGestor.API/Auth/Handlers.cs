using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SAMGestor.Application.Common.Auth;

namespace SAMGestor.API.Auth;

public sealed class ReadOnlyHandler : AuthorizationHandler<ReadOnlyRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ReadOnlyRequirement requirement)
    {
        var role = context.User.FindFirstValue(ClaimTypes.Role) ?? context.User.FindFirstValue("role");
        if (role is RoleNames.Consultant or RoleNames.Manager or RoleNames.Admin)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public sealed class ManageAllButDeleteUsersHandler : AuthorizationHandler<ManageAllButDeleteUsersRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ManageAllButDeleteUsersRequirement requirement)
    {
        var role = context.User.FindFirstValue(ClaimTypes.Role) ?? context.User.FindFirstValue("role");
        if (role is RoleNames.Manager or RoleNames.Admin)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public sealed class AdminOnlyHandler : AuthorizationHandler<AdminOnlyRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AdminOnlyRequirement requirement)
    {
        var role = context.User.FindFirstValue(ClaimTypes.Role) ?? context.User.FindFirstValue("role");
        if (role == RoleNames.Admin)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public sealed class EmailConfirmedHandler : AuthorizationHandler<EmailConfirmedRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, EmailConfirmedRequirement requirement)
    {
        var confirmed = context.User.FindFirstValue("email_confirmed");
        if (bool.TryParse(confirmed, out var ok) && ok)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
