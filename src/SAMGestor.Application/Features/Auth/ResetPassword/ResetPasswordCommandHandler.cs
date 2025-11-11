using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Auth.ResetPassword;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Unit>
{
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokens,
        IUserRepository users,
        IPasswordHasher hasher,
        IDateTimeProvider clock,
        IUnitOfWork uow)
    {
        _tokens = tokens;
        _users = users;
        _hasher = hasher;
        _clock = clock;
        _uow = uow;
    }

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token)) throw new InvalidOperationException("Token é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.NewPassword)) throw new InvalidOperationException("Nova senha é obrigatória.");

        var now = _clock.UtcNow;
        var hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(request.Token)));
        var token = await _tokens.GetByHashAsync(hash, ct) ?? throw new UnauthorizedAccessException("Token inválido.");

        if (!token.IsValid(now)) throw new UnauthorizedAccessException("Token expirado ou já utilizado.");

        var user = await _users.GetByIdAsync(token.UserId, ct) ?? throw new InvalidOperationException("Usuário não encontrado.");
        var newHash = _hasher.Hash(request.NewPassword);
        user.ChangePassword(new Domain.ValueObjects.PasswordHash(newHash));
        token.MarkUsed(now);

        await _users.UpdateAsync(user, ct);
        await _tokens.UpdateAsync(token, ct);
        await _uow.SaveChangesAsync(ct);

        return Unit.Value;
    }
}
