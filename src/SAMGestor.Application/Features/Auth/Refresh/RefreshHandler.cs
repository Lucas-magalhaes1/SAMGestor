using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Dtos.Auth;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Auth.Refresh;

public sealed class RefreshHandler : IRequestHandler<RefreshCommand, RefreshResponse>
{
    private readonly IRefreshTokenService _refreshService;
    private readonly IJwtTokenService _jwtService;
    private readonly IJwtTokenDecoder _jwtDecoder;
    private readonly IRefreshTokenRepository _refreshRepo;
    private readonly IUserRepository _userRepo;
    private readonly IDateTimeProvider _time;
    private readonly IUnitOfWork _uow;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<RefreshHandler> _logger;

    public RefreshHandler(
        IRefreshTokenService refreshService,
        IJwtTokenService jwtService,
        IJwtTokenDecoder jwtDecoder,
        IRefreshTokenRepository refreshRepo,
        IUserRepository userRepo,
        IDateTimeProvider time,
        IUnitOfWork uow,
        IOptions<JwtOptions> jwtOptions,
        ILogger<RefreshHandler> logger)
    {
        _refreshService = refreshService;
        _jwtService = jwtService;
        _jwtDecoder = jwtDecoder;
        _refreshRepo = refreshRepo;
        _userRepo = userRepo;
        _time = time;
        _uow = uow;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<RefreshResponse> Handle(RefreshCommand req, CancellationToken ct)
    {
        var now = _time.UtcNow;
        var gracePeriodSeconds = _jwtOptions.RefreshTokenReusePeriodSeconds;

        Guid userId;
        try
        {
            userId = _jwtDecoder.ExtractUserIdFromExpiredToken(req.AccessToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to decode access token during refresh");
            throw new UnauthorizedAccessException("Invalid access token");
        }

        var tokenHash = _refreshService.Hash(req.RefreshToken);
        var oldToken = await _refreshRepo.GetByHashWithLockAsync(userId, tokenHash, ct);

        if (oldToken == null)
        {
            _logger.LogWarning("Invalid refresh token hash for user {UserId}", userId);
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        if (!oldToken.IsActive(now))
        {
            _logger.LogWarning("Expired or revoked refresh token for user {UserId}", userId);
            throw new UnauthorizedAccessException("Refresh token expired or revoked");
        }

        if (oldToken.ReplacedByTokenId.HasValue && oldToken.UsedAt.HasValue)
        {
            var timeSinceUsed = (now - oldToken.UsedAt.Value).TotalSeconds;

            _logger.LogInformation(
                "Token reuse detected for user {UserId}. Time since first use: {TimeSinceUsed:F2}s. Grace period: {GracePeriod}s",
                userId, timeSinceUsed, gracePeriodSeconds);

            if (timeSinceUsed <= gracePeriodSeconds)
            {
                _logger.LogInformation(
                    "Concurrent refresh request within grace period for user {UserId}. Attempting to return cached replacement token.",
                    userId);

                var replacementToken = await _refreshRepo.GetByIdAsync(oldToken.ReplacedByTokenId.Value, ct);

                if (replacementToken != null && replacementToken.IsActive(now))
                {
                    var cachedRawToken = await _refreshService.GetRawTokenByIdAsync(replacementToken.Id, ct);

                    if (!string.IsNullOrEmpty(cachedRawToken))
                    {
                        var user = await _userRepo.GetByIdAsync(userId, ct);
                        if (user == null || !user.Enabled)
                        {
                            _logger.LogWarning("User {UserId} not found or disabled during concurrent refresh", userId);
                            throw new UnauthorizedAccessException("User not found or disabled");
                        }

                        var newAccessToken = _jwtService.GenerateAccessToken(user, now);

                        _logger.LogInformation(
                            "Successfully returned cached replacement token for concurrent request. User {UserId}",
                            userId);

                        return new RefreshResponse(newAccessToken, cachedRawToken);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "Cache miss for replacement token {TokenId}. Instructing client to retry. User {UserId}",
                            replacementToken.Id, userId);

                        throw new UnauthorizedAccessException(
                            "Concurrent refresh detected. Please retry in a moment.");
                    }
                }
            }

            _logger.LogWarning(
                "Suspicious token reuse detected for user {UserId} OUTSIDE grace period ({TimeSinceUsed:F2}s > {GracePeriod}s). Revoking all sessions.",
                userId, timeSinceUsed, gracePeriodSeconds);

            var allTokens = await _refreshRepo.GetActiveTokensByUserIdAsync(userId, now, ct);
            foreach (var token in allTokens)
            {
                token.Revoke(now);
                await _refreshRepo.UpdateAsync(token, ct);
            }
            await _uow.SaveChangesAsync(ct);

            _logger.LogWarning(
                "Revoked {TokenCount} active tokens for user {UserId} due to suspicious activity",
                allTokens.Count, userId);

            throw new UnauthorizedAccessException(
                "Token reuse detected. All sessions have been revoked for security. Please login again.");
        }

        var currentUser = await _userRepo.GetByIdAsync(userId, ct);
        if (currentUser == null)
        {
            _logger.LogWarning("User {UserId} not found during refresh", userId);
            throw new UnauthorizedAccessException("User not found");
        }

        if (!currentUser.Enabled)
        {
            _logger.LogWarning("Disabled user {UserId} attempted to refresh token", userId);
            throw new UnauthorizedAccessException("User account is disabled");
        }

        var newAccessToken2 = _jwtService.GenerateAccessToken(currentUser, now);
        var (newRefreshRaw, newRefreshEntity) = await _refreshService.GenerateAsync(
            currentUser, now, req.UserAgent, req.IpAddress);

        await _refreshRepo.AddAsync(newRefreshEntity, ct);
        await _uow.SaveChangesAsync(ct);

        oldToken.MarkAsUsed(newRefreshEntity.Id, now);
        await _refreshRepo.UpdateAsync(oldToken, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Token refreshed successfully for user {UserId}. Old token enters {GracePeriod}s grace period.",
            userId, gracePeriodSeconds);

        return new RefreshResponse(newAccessToken2, newRefreshRaw);
    }
}
