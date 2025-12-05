using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Dtos.Auth;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Entities;
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
    private readonly ILogger<RefreshHandler> _logger;

    public RefreshHandler(
        IRefreshTokenService refreshService,
        IJwtTokenService jwtService,
        IJwtTokenDecoder jwtDecoder,
        IRefreshTokenRepository refreshRepo,
        IUserRepository userRepo,
        IDateTimeProvider time,
        IUnitOfWork uow,
        ILogger<RefreshHandler> logger)
    {
        _refreshService = refreshService;
        _jwtService = jwtService;
        _jwtDecoder = jwtDecoder;
        _refreshRepo = refreshRepo;
        _userRepo = userRepo;
        _time = time;
        _uow = uow;
        _logger = logger;
    }

    public async Task<RefreshResponse> Handle(RefreshCommand req, CancellationToken ct)
    {
        var now = _time.UtcNow;

        // 1. Extrair userId do access token (pode estar expirado)
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

        // 2. Validar o refresh token
        RefreshToken oldToken;
        try
        {
            oldToken = await _refreshService.ValidateAsync(req.RefreshToken, userId, now, ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Invalid refresh token for user {UserId}", userId);
            throw;
        }

        // 3. DETECÇÃO DE REUTILIZAÇÃO: Se já foi substituído, token comprometido!
        if (oldToken.ReplacedByTokenId.HasValue)
        {
            _logger.LogWarning(
                "Refresh token reuse detected for user {UserId}. Token was already replaced by {NewTokenId}. Revoking all sessions.",
                userId, oldToken.ReplacedByTokenId.Value);

            // Revogar TODOS os tokens ativos do usuário
            var allTokens = await _refreshRepo.GetActiveTokensByUserIdAsync(userId, now, ct);
            foreach (var token in allTokens)
            {
                token.Revoke(now);
                await _refreshRepo.UpdateAsync(token, ct);
            }
            await _uow.SaveChangesAsync(ct);

            throw new UnauthorizedAccessException(
                "Token reuse detected. All sessions have been revoked for security. Please login again.");
        }

        // 4. Buscar usuário
        var user = await _userRepo.GetByIdAsync(userId, ct);
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found during refresh", userId);
            throw new UnauthorizedAccessException("User not found");
        }

        if (!user.Enabled)
        {
            _logger.LogWarning("Disabled user {UserId} attempted to refresh token", userId);
            throw new UnauthorizedAccessException("User account is disabled");
        }

        // 5. Gerar novo par de tokens
        var newAccessToken = _jwtService.GenerateAccessToken(user, now);
        var (newRefreshRaw, newRefreshEntity) = await _refreshService.GenerateAsync(
            user, now, req.UserAgent, req.IpAddress);

        // 6. Marcar o token antigo como substituído pelo novo
        oldToken.ReplaceWith(newRefreshEntity.Id, now);
        await _refreshRepo.UpdateAsync(oldToken, ct);

        // 7. Persistir novo refresh token
        await _refreshRepo.AddAsync(newRefreshEntity, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Token refreshed successfully for user {UserId}", userId);

        return new RefreshResponse(newAccessToken, newRefreshRaw);
    }
}
