using MediatR;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Dtos.Auth;
using SAMGestor.Application.Dtos.Users;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Auth.ConfirmEmail;

public sealed class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, LoginResponse>
{
    private readonly IEmailConfirmationTokenRepository _tokens;
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenService _refresh;
    private readonly IRefreshTokenRepository _refreshRepo;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public ConfirmEmailCommandHandler(
        IEmailConfirmationTokenRepository tokens,
        IUserRepository users,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IRefreshTokenService refresh,
        IRefreshTokenRepository refreshRepo,
        IUnitOfWork uow,
        IDateTimeProvider clock)
    {
        _tokens = tokens;
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
        _refresh = refresh;
        _refreshRepo = refreshRepo;
        _uow = uow;
        _clock = clock;
    }

    public async Task<LoginResponse> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Token)) throw new InvalidOperationException("Token é obrigatório.");
        if (string.IsNullOrWhiteSpace(request.NewPassword)) throw new InvalidOperationException("Nova senha é obrigatória.");

        var now = _clock.UtcNow;

        var tokenHash = _refresh.Hash(request.Token); // reuso do hash SHA256
        var token = await _tokens.GetByHashAsync(tokenHash, ct);
        if (token is null || !token.IsValid(now)) throw new UnauthorizedAccessException("Token inválido ou expirado.");

        var user = await _users.GetByIdAsync(token.UserId, ct) ?? throw new InvalidOperationException("Usuário não encontrado.");
        // define a senha e confirma e-mail
        var newHash = _hasher.Hash(request.NewPassword);
        user.ChangePassword(new Domain.ValueObjects.PasswordHash(newHash));
        user.ConfirmEmail(now);
        token.MarkUsed(now);

        var access = _jwt.GenerateAccessToken(user, now);
        var (rawRefresh, entity) = await _refresh.GenerateAsync(user, now, null, null);
        await _refreshRepo.AddAsync(entity, ct);
        await _users.UpdateAsync(user, ct);
        await _tokens.UpdateAsync(token, ct);
        await _uow.SaveChangesAsync(ct);

        var summary = new UserSummary(user.Id, user.Name.Value, user.Email.Value, user.Role.ToShortName());
        return new LoginResponse("Login successful", true, access, rawRefresh, user.EmailConfirmed, summary);
    }
}
