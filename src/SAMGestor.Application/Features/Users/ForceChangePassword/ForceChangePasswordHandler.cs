using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Common.Validators;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Contracts;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Users.ForceChangePassword;

public sealed class ForceChangePasswordHandler : IRequestHandler<ForceChangePasswordCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;
    private readonly IEventBus _bus;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ForceChangePasswordHandler> _logger;

    public ForceChangePasswordHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        IRefreshTokenRepository refreshTokens,
        IDateTimeProvider clock,
        IUnitOfWork uow,
        IEventBus bus,
        ICurrentUser currentUser,
        ILogger<ForceChangePasswordHandler> logger)
    {
        _users = users;
        _hasher = hasher;
        _refreshTokens = refreshTokens;
        _clock = clock;
        _uow = uow;
        _bus = bus;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Unit> Handle(ForceChangePasswordCommand request, CancellationToken ct)
    {
        // 1. Buscar usuário alvo
        var user = await _users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            throw new KeyNotFoundException($"Usuário {request.UserId} não encontrado");

        // 2. ⚠️ VALIDAR FORÇA DA SENHA
        var (isValid, errorMessage) = PasswordValidator.Validate(request.NewPassword);
        if (!isValid)
            throw new InvalidOperationException(errorMessage);

        // 3. ⚠️ VERIFICAR SE CONTÉM DADOS PESSOAIS
        if (PasswordValidator.ContainsPersonalInfo(request.NewPassword, user.Name.Value, user.Email.Value))
        {
            throw new InvalidOperationException("A senha não pode conter o nome ou e-mail do usuário");
        }

        var now = _clock.UtcNow;

        // 4. ✅ ALTERAR SENHA
        var newHash = _hasher.Hash(request.NewPassword);
        user.ChangePassword(new PasswordHash(newHash));
        
        // Resetar lockout
        user.MarkLoginSuccess(now);
        
        await _users.UpdateAsync(user, ct);

        // 5. ⚠️ REVOGAR TODOS OS REFRESH TOKENS (forçar logout)
        var activeTokens = await _refreshTokens.GetActiveTokensByUserIdAsync(user.Id, now, ct);
        foreach (var token in activeTokens)
        {
            token.Revoke(now);
            await _refreshTokens.UpdateAsync(token, ct);
        }

        // 6. Publicar evento para notificar usuário
        await _bus.EnqueueAsync(
            type: EventTypes.PasswordChangedByAdminV1,
            source: "sam.core",
            data: new PasswordChangedByAdminV1(
                UserId: user.Id,
                Name: user.Name.Value,
                Email: user.Email.Value,
                ChangedBy: _currentUser.Email ?? "Admin"
            ),
            ct: ct
        );

        await _uow.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Admin {AdminId} alterou senha do usuário {UserId} e revogou {TokenCount} sessões ativas",
            _currentUser.UserId, user.Id, activeTokens.Count);

        return Unit.Value;
    }
}
