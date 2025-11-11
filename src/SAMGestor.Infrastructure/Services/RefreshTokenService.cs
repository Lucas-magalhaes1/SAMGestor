using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Services;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly IOpaqueTokenGenerator _opaque;
    private readonly JwtOptions _opt;

    public RefreshTokenService(IOpaqueTokenGenerator opaque, IOptions<JwtOptions> options)
    {
        _opaque = opaque;
        _opt = options.Value;
    }

    public async Task<(string RawToken, RefreshToken Entity)> GenerateAsync(
        User user,
        DateTimeOffset now,
        string? userAgent = null,
        string? ipAddress = null)
    {
        // raw opaco, URL-safe
        var raw = _opaque.GenerateSecureToken(64);
        var hash = Hash(raw);

        var entity = RefreshToken.Create(
            userId: user.Id,
            tokenHash: hash,
            expiresAt: now.AddDays(_opt.RefreshTokenDays),
            createdAt: now,
            userAgent: userAgent,
            ip: ipAddress
        );

        // “async gap” para manter assinatura async (se precisar IO futuramente)
        await Task.CompletedTask;
        return (raw, entity);
    }

    public string Hash(string rawToken)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}