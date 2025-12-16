using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace SAMGestor.IntegrationTests.Shared;

public sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "Test";

#pragma warning disable CS0618 // ISystemClock obsolete - ok em testes
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock
    ) : base(options, logger, encoder, clock)
    { }
#pragma warning restore CS0618

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = Guid.NewGuid().ToString();
        var email = "admin@test.local";
        var name = "Integration Test Admin";
        var role = "Admin";
        var emailConfirmed = "true";
        
        if (Request.Headers.TryGetValue("X-Test-UserId", out var hUserId) && Guid.TryParse(hUserId, out _))
            userId = hUserId!;

        if (Request.Headers.TryGetValue("X-Test-Email", out var hEmail) && !string.IsNullOrWhiteSpace(hEmail))
            email = hEmail!;

        if (Request.Headers.TryGetValue("X-Test-Name", out var hName) && !string.IsNullOrWhiteSpace(hName))
            name = hName!;

        if (Request.Headers.TryGetValue("X-Test-Role", out var hRole) && !string.IsNullOrWhiteSpace(hRole))
            role = hRole!;

        if (Request.Headers.TryGetValue("X-Test-EmailConfirmed", out var hConfirmed) && !string.IsNullOrWhiteSpace(hConfirmed))
            emailConfirmed = hConfirmed!;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId),
            new("sub", userId),

            new(ClaimTypes.Email, email),
            new("email", email),

            new(ClaimTypes.Name, name),

            new(ClaimTypes.Role, role),
            new("role", role),

            new("email_confirmed", emailConfirmed.ToLowerInvariant())
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
