using SAMGestor.Application.Dtos.Users;

namespace SAMGestor.Application.Dtos.Auth;

public sealed record LoginResponse(
    string Message,
    bool Success,
    string AccessToken,
    string RefreshToken,
    bool EmailConfirmed,
    UserSummary? User
);