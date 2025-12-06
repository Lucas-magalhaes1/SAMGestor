using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Common.Auth;
using SAMGestor.Application.Common.Validators;
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
    private readonly ILogger<ConfirmEmailCommandHandler> _logger;

    public ConfirmEmailCommandHandler(
        IEmailConfirmationTokenRepository tokens,
        IUserRepository users,
        IPasswordHasher hasher,
        IJwtTokenService jwt,
        IRefreshTokenService refresh,
        IRefreshTokenRepository refreshRepo,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ILogger<ConfirmEmailCommandHandler> logger)
    {
        _tokens = tokens;
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
        _refresh = refresh;
        _refreshRepo = refreshRepo;
        _uow = uow;
        _clock = clock;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(ConfirmEmailCommand request, CancellationToken ct)
    {
        // 1. Validações básicas
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new InvalidOperationException("Token de confirmação é obrigatório");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            throw new InvalidOperationException("Nova senha é obrigatória");

        var now = _clock.UtcNow;

        // 2. Buscar e validar token
        var tokenHash = _refresh.Hash(request.Token);
        var token = await _tokens.GetByHashAsync(tokenHash, ct);
        
        if (token == null)
        {
            _logger.LogWarning("Token de confirmação inválido ou não encontrado");
            throw new UnauthorizedAccessException("Token de confirmação inválido ou expirado");
        }

        if (!token.IsValid(now))
        {
            _logger.LogWarning("Token de confirmação expirado para usuário {UserId}", token.UserId);
            throw new UnauthorizedAccessException("Token de confirmação expirado. Solicite um novo convite");
        }

        // 3. Buscar usuário
        var user = await _users.GetByIdAsync(token.UserId, ct);
        if (user == null)
        {
            _logger.LogError("Usuário {UserId} não encontrado para token de confirmação", token.UserId);
            throw new InvalidOperationException("Usuário não encontrado");
        }

        // 4.  VALIDAR FORÇA DA SENHA
        var (isValid, errorMessage) = PasswordValidator.Validate(request.NewPassword);
        if (!isValid)
            throw new InvalidOperationException(errorMessage);

        // 5. VERIFICAR SE CONTÉM DADOS PESSOAIS
        if (PasswordValidator.ContainsPersonalInfo(request.NewPassword, user.Name.Value, user.Email.Value))
        {
            throw new InvalidOperationException("A senha não pode conter seu nome ou e-mail");
        }

        // 6. Verificar se e-mail já foi confirmado
        if (user.EmailConfirmed)
        {
            _logger.LogWarning("Tentativa de confirmar e-mail já confirmado para usuário {UserId}", user.Id);
            throw new InvalidOperationException("E-mail já foi confirmado. Faça login normalmente");
        }

        // 7.  DEFINIR SENHA e CONFIRMAR E-MAIL
        var newHash = _hasher.Hash(request.NewPassword);
        user.ChangePassword(new Domain.ValueObjects.PasswordHash(newHash));
        user.ConfirmEmail(now);
        token.MarkUsed(now);

        // 8. Gerar tokens de acesso (login automático)
        var access = _jwt.GenerateAccessToken(user, now);
        var (rawRefresh, entity) = await _refresh.GenerateAsync(user, now, null, null);
        
        await _refreshRepo.AddAsync(entity, ct);
        await _users.UpdateAsync(user, ct);
        await _tokens.UpdateAsync(token, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("E-mail confirmado e senha definida para usuário {UserId}", user.Id);

        var summary = new UserSummary(user.Id, user.Name.Value, user.Email.Value, user.Role.ToShortName());
        return new LoginResponse(
            Message: "Conta ativada com sucesso! Bem-vindo ao SAMGestor",
            Success: true,
            AccessToken: access,
            RefreshToken: rawRefresh,
            EmailConfirmed: user.EmailConfirmed,
            User: summary
        );
    }
}
