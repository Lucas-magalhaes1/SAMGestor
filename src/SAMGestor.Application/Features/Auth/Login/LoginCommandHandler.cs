using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
    private readonly LockoutOptions _lockoutOpts;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository users,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IRefreshTokenService refresh,
        IRefreshTokenRepository refreshRepo,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        IOptions<LockoutOptions> lockoutOpts,
        ILogger<LoginCommandHandler> logger)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
        _refresh = refresh;
        _refreshRepo = refreshRepo;
        _uow = uow;
        _clock = clock;
        _lockoutOpts = lockoutOpts.Value;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        // 1. Validação básica
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("E-mail e senha são obrigatórios");

        var now = _clock.UtcNow;

        // 2. Buscar usuário
        var user = await _users.GetByEmailAsync(request.Email, ct);
        if (user is null)
        {
            _logger.LogWarning("Tentativa de login para e-mail inexistente: {Email}", request.Email);
            // Delay proposital para dificultar enumeração de e-mails
            await Task.Delay(Random.Shared.Next(100, 300), ct);
            throw new UnauthorizedAccessException("Credenciais inválidas");
        }

        // 3.  VERIFICAR SE CONTA ESTÁ DESABILITADA (bloqueada pelo admin)
        if (!user.Enabled)
        {
            _logger.LogWarning("Tentativa de login em conta desabilitada: {UserId}", user.Id);
            throw new UnauthorizedAccessException("Conta desabilitada. Entre em contato com o administrador");
        }

        // 4.  VERIFICAR LOCKOUT TEMPORÁRIO (por tentativas falhas)
        if (user.IsLocked(now))
        {
            var remainingTime = user.LockoutEndAt!.Value - now;
            var minutesLeft = Math.Ceiling(remainingTime.TotalMinutes);
            
            _logger.LogWarning(
                "Tentativa de login em conta bloqueada temporariamente: {UserId}. Tempo restante: {Minutes}min",
                user.Id, minutesLeft);

            throw new UnauthorizedAccessException(
                $"Conta temporariamente bloqueada devido a múltiplas tentativas falhas. " +
                $"Tente novamente em {minutesLeft} minuto(s)");
        }

        // 5. Verificar senha
        if (!_hasher.Verify(user.PasswordHash.Value, request.Password))
        {
            //  REGISTRAR FALHA DE LOGIN
            user.RegisterFailedLogin(
                now,
                _lockoutOpts.MaxFailedAttempts,
                TimeSpan.FromMinutes(_lockoutOpts.LockoutDurationMinutes));

            await _users.UpdateAsync(user, ct);
            await _uow.SaveChangesAsync(ct);

            // Verificar se acabou de ser bloqueado
            if (user.IsLocked(now))
            {
                _logger.LogWarning(
                    "Usuário {UserId} foi bloqueado temporariamente após {Attempts} tentativas falhas",
                    user.Id, _lockoutOpts.MaxFailedAttempts);

                throw new UnauthorizedAccessException(
                    $"Credenciais inválidas. Conta bloqueada por {_lockoutOpts.LockoutDurationMinutes} minutos " +
                    "devido a múltiplas tentativas falhas");
            }

            // Informar tentativas restantes (segurança: só se < 3)
            var attemptsLeft = _lockoutOpts.MaxFailedAttempts - user.FailedAccessCount;
            
            _logger.LogWarning(
                "Falha de login para usuário {UserId}. Tentativas restantes: {AttemptsLeft}",
                user.Id, attemptsLeft);

            // Só mostra tentativas restantes se estiver perto do limite
            var message = attemptsLeft <= 2
                ? $"Credenciais inválidas. {attemptsLeft} tentativa(s) restante(s) antes do bloqueio temporário"
                : "Credenciais inválidas";

            throw new UnauthorizedAccessException(message);
        }

        // 6. LOGIN BEM-SUCEDIDO
        user.MarkLoginSuccess(now);
        await _users.UpdateAsync(user, ct);

        // 7. Gerar tokens
        var access = _jwt.GenerateAccessToken(user, now);
        var (rawRefresh, entity) = await _refresh.GenerateAsync(
            user, now, request.UserAgent, request.Ip);
        
        await _refreshRepo.AddAsync(entity, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Usuário {UserId} ({Email}) realizou login com sucesso", user.Id, user.Email.Value);

        var summary = new UserSummary(
            user.Id, 
            user.Name.Value, 
            user.Email.Value, 
            user.Role.ToShortName());

        return new LoginResponse(
            Message: "Login realizado com sucesso",
            Success: true,
            AccessToken: access,
            RefreshToken: rawRefresh,
            EmailConfirmed: user.EmailConfirmed,
            User: summary
        );
    }
}
