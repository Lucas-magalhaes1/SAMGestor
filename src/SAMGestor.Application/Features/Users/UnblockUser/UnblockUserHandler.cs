using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.UnblockUser;

public sealed class UnblockUserHandler : IRequestHandler<UnblockUserCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<UnblockUserHandler> _logger;

    public UnblockUserHandler(
        IUserRepository users,
        IDateTimeProvider clock,
        IUnitOfWork uow,
        ICurrentUser currentUser,
        ILogger<UnblockUserHandler> logger)
    {
        _users = users;
        _clock = clock;
        _uow = uow;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Unit> Handle(UnblockUserCommand request, CancellationToken ct)
    {
        var user = await _users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            throw new KeyNotFoundException($"Usuário {request.UserId} não encontrado");

        if (user.Enabled)
            throw new InvalidOperationException("Usuário já está ativo");

        var now = _clock.UtcNow;

        // Habilitar conta e limpar lockout temporário
        user.Enable();
        user.MarkLoginSuccess(now); // Zera FailedAccessCount e LockoutEndAt
        
        await _users.UpdateAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Admin {AdminId} desbloqueou usuário {UserId} ({Email})",
            _currentUser.UserId, user.Id, user.Email.Value);

        return Unit.Value;
    }
}