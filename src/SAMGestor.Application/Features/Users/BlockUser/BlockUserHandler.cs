using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.BlockUser;

public sealed class BlockUserHandler : IRequestHandler<BlockUserCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<BlockUserHandler> _logger;

    public BlockUserHandler(
        IUserRepository users,
        IRefreshTokenRepository refreshTokens,
        IDateTimeProvider clock,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        ILogger<BlockUserHandler> logger)
    {
        _users = users;
        _refreshTokens = refreshTokens;
        _clock = clock;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Unit> Handle(BlockUserCommand request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            throw new KeyNotFoundException($"Usuário {request.UserId} não encontrado");

        if (!user.Enabled)
            throw new InvalidOperationException("Usuário já está bloqueado");

        var now = _clock.UtcNow;

        // Desabilitar conta
        user.Disable();
        await _users.UpdateAsync(user, ct);

        // Revogar todas as sessões ativas
        var activeTokens = await _refreshTokens.GetActiveTokensByUserIdAsync(user.Id, now, ct);
        foreach (var token in activeTokens)
        {
            token.Revoke(now);
            await _refreshTokens.UpdateAsync(token, ct);
        }

        await _uow.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Admin {AdminId} bloqueou usuário {UserId} ({Email}). {TokenCount} sessões revogadas",
            _currentUser.UserId, user.Id, user.Email.Value, activeTokens.Count);

        return Unit.Value;
    }
}
