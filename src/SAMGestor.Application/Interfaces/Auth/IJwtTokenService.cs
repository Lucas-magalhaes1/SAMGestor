using SAMGestor.Domain.Entities;

namespace SAMGestor.Application.Interfaces.Auth;

/// <summary>
/// Gera o Access Token (JWT) com claims de user.
/// </summary>
public interface IJwtTokenService
{
    string GenerateAccessToken(User user, DateTimeOffset now);
}