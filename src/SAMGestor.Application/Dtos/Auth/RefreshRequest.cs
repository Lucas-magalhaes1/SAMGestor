namespace SAMGestor.Application.Dtos.Auth;

/// <summary>
/// Request para refresh de tokens.
/// AccessToken é usado apenas para extrair userId (pode estar expirado).
/// </summary>
public sealed record RefreshRequest(
    string AccessToken,
    string RefreshToken
);

public sealed record RefreshResponse(string AccessToken, string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);