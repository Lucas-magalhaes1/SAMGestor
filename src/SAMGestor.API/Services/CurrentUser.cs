using System.Security.Claims;
using SAMGestor.Application.Interfaces.Auth;

namespace SAMGestor.API.Services;

public sealed class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;

    public CurrentUser(IHttpContextAccessor http) => _http = http;

    public Guid? UserId
        => Guid.TryParse(_http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? _http.HttpContext?.User.FindFirstValue("sub"), out var id)
            ? id : null;

    public string? Email
        => _http.HttpContext?.User.FindFirstValue(ClaimTypes.Email)
           ?? _http.HttpContext?.User.FindFirstValue("email");

    public string? Role
        => _http.HttpContext?.User.FindFirstValue(ClaimTypes.Role)
           ?? _http.HttpContext?.User.FindFirstValue("role");

    public bool? EmailConfirmed
        => bool.TryParse(_http.HttpContext?.User.FindFirstValue("email_confirmed"), out var v) ? v : null;
}