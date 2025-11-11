using SAMGestor.Domain.Entities;

namespace SAMGestor.Application.Interfaces.Auth;

/// <summary>
/// Gera o refresh token (opaco) + entidade persistível, e faz hash/rotação.
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Gera um novo refresh (raw) + entidade, sem persistir.
    /// </summary>
    Task<(string RawToken, RefreshToken Entity)> GenerateAsync(
        User user,
        DateTimeOffset now,
        string? userAgent = null,
        string? ipAddress = null
    );

    /// <summary>
    /// Calcula o hash (ex.: SHA256) de um token opaco para armazenamento.
    /// </summary>
    string Hash(string rawToken);
}