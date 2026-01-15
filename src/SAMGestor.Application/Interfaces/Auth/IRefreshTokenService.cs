using SAMGestor.Domain.Entities;

namespace SAMGestor.Application.Interfaces.Auth;

public interface IRefreshTokenService
{
    
    Task<(string RawToken, RefreshToken Entity)> GenerateAsync(
        User user,
        DateTimeOffset now,
        string? userAgent = null,
        string? ipAddress = null
    );

    string Hash(string rawToken);
    
    Task<RefreshToken> ValidateAsync(
        string rawToken,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct = default
    );
    
    Task<string?> GetRawTokenByIdAsync(Guid tokenId, CancellationToken ct = default);
}