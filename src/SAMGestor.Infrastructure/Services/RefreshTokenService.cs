
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Infrastructure.Services;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly IOpaqueTokenGenerator _opaque;
    private readonly IRefreshTokenRepository _repo;
    private readonly IDistributedCache _cache;
    private readonly JwtOptions _opt;
    private readonly ILogger<RefreshTokenService> _logger;

    public RefreshTokenService(
        IOpaqueTokenGenerator opaque, 
        IRefreshTokenRepository repo,
        IDistributedCache cache,
        IOptions<JwtOptions> options,
        ILogger<RefreshTokenService> logger)
    {
        _opaque = opaque;
        _repo = repo;
        _cache = cache;
        _opt = options.Value;
        _logger = logger;
    }

    public async Task<(string RawToken, RefreshToken Entity)> GenerateAsync(
        User user,
        DateTimeOffset now,
        string? userAgent = null,
        string? ipAddress = null)
    {
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
        
        var cacheKey = GetCacheKey(entity.Id);
        var cacheExpiry = TimeSpan.FromSeconds(_opt.RefreshTokenReusePeriodSeconds + 60);
        
        try
        {
            await _cache.SetStringAsync(
                cacheKey, 
                raw, 
                new DistributedCacheEntryOptions 
                { 
                    AbsoluteExpirationRelativeToNow = cacheExpiry 
                });

            _logger.LogDebug(
                "Raw refresh token cached for {Expiry}s with key {CacheKey}",
                cacheExpiry.TotalSeconds, cacheKey);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, 
                "Failed to cache raw refresh token for token {TokenId}. Grace period may not work.",
                entity.Id);
        }

        return (raw, entity);
    }

    public string Hash(string rawToken)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public async Task<RefreshToken> ValidateAsync(
        string rawToken,
        Guid userId,
        DateTimeOffset now,
        CancellationToken ct = default)
    {
        var hash = Hash(rawToken);
        var token = await _repo.GetByHashAsync(userId, hash, ct);

        if (token == null)
            throw new UnauthorizedAccessException("Invalid refresh token");

        if (!token.IsActive(now))
            throw new UnauthorizedAccessException("Refresh token expired or revoked");

        return token;
    }
    
    public async Task<string?> GetRawTokenByIdAsync(Guid tokenId, CancellationToken ct = default)
    {
        var cacheKey = GetCacheKey(tokenId);
        
        try
        {
            var rawToken = await _cache.GetStringAsync(cacheKey, ct);
            
            if (rawToken != null)
            {
                _logger.LogDebug("Raw token found in cache for {TokenId}", tokenId);
            }
            else
            {
                _logger.LogDebug("Raw token NOT found in cache for {TokenId}", tokenId);
            }

            return rawToken;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve raw token from cache for {TokenId}", tokenId);
            return null;
        }
    }

    private static string GetCacheKey(Guid tokenId) => $"refresh_token_raw:{tokenId}";
}
