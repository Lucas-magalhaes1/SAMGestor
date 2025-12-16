using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace SAMGestor.IntegrationTests.Shared;

public sealed class AllowAllAuthorizationService : IAuthorizationService
{
    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        IEnumerable<IAuthorizationRequirement> requirements)
        => Task.FromResult(AuthorizationResult.Success());

    public Task<AuthorizationResult> AuthorizeAsync(
        ClaimsPrincipal user,
        object? resource,
        string policyName)
        => Task.FromResult(AuthorizationResult.Success());
}