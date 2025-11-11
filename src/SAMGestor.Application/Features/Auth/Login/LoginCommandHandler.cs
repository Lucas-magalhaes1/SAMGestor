using MediatR;
using SAMGestor.Application.Common;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Dtos.Auth;
using SAMGestor.Application.Dtos.Users;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Auth.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenService _jwt;
    private readonly IRefreshTokenService _refresh;
    private readonly IRefreshTokenRepository _refreshRepo;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    public LoginCommandHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IRefreshTokenService refresh,
        IRefreshTokenRepository refreshRepo,
        IUnitOfWork uow,
        IDateTimeProvider clock)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
        _refresh = refresh;
        _refreshRepo = refreshRepo;
        _uow = uow;
        _clock = clock;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Missing email or password"); // middleware mapeia p/ 400

        var user = await _users.GetByEmailAsync(request.Email, ct);
        if (user is null || !_hasher.Verify(user.PasswordHash.Value, request.Password))
            throw new UnauthorizedAccessException("Invalid credentials");

        var now = _clock.UtcNow;

        // sucesso de login
        user.MarkLoginSuccess(now);
        // gera tokens
        var access = _jwt.GenerateAccessToken(user, now);
        var (rawRefresh, entity) = await _refresh.GenerateAsync(user, now, request.UserAgent, request.Ip);
        await _refreshRepo.AddAsync(entity, ct);
        await _users.UpdateAsync(user, ct);
        await _uow.SaveChangesAsync(ct);

        var summary = new UserSummary(user.Id, user.Name.Value, user.Email.Value, user.Role.ToShortName());
        return new LoginResponse(
            Message: "Login successful",
            Success: true,
            AccessToken: access,
            RefreshToken: rawRefresh,
            EmailConfirmed: user.EmailConfirmed,
            User: summary
        );
    }
}
