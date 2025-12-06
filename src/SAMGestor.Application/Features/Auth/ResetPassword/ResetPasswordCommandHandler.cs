using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Common.Validators;
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
    private readonly ILogger<ResetPasswordCommandHandler> _logger;

    public ResetPasswordCommandHandler(
        IPasswordResetTokenRepository tokens,
        IUserRepository users,
        IPasswordHasher hasher,
        IDateTimeProvider clock,
        IUnitOfWork uow,
        ILogger<ResetPasswordCommandHandler> logger)
    {
        _tokens = tokens;
        _users = users;
        _hasher = hasher;
        _clock = clock;
        _uow = uow;
        _logger = logger;
    }

    public async Task<Unit> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        // 1. Validações básicas
        if (string.IsNullOrWhiteSpace(request.Token))
            throw new InvalidOperationException("Token de reset é obrigatório");

        if (string.IsNullOrWhiteSpace(request.NewPassword))
            throw new InvalidOperationException("Nova senha é obrigatória");

        var now = _clock.UtcNow;

        // 2. Buscar e validar token
        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(request.Token)));

        var token = await _tokens.GetByHashAsync(tokenHash, ct);

        if (token == null)
        {
            _logger.LogWarning("Token de reset inválido ou não encontrado");
            throw new UnauthorizedAccessException("Token de reset inválido ou expirado");
        }

        if (!token.IsValid(now))
        {
            _logger.LogWarning("Token de reset expirado para usuário {UserId}", token.UserId);
            throw new UnauthorizedAccessException("Token de reset expirado. Solicite um novo link");
        }

        // 3. Buscar usuário
        var user = await _users.GetByIdAsync(token.UserId, ct);
        if (user == null)
        {
            _logger.LogError("Usuário {UserId} não encontrado para token de reset", token.UserId);
            throw new InvalidOperationException("Usuário não encontrado");
        }

        // 4. Verificar se conta está habilitada
        if (!user.Enabled)
        {
            _logger.LogWarning("Tentativa de reset em conta desabilitada: {UserId}", user.Id);
            throw new UnauthorizedAccessException("Conta desabilitada. Entre em contato com o administrador.");
        }

        // 5.  VALIDAR FORÇA DA SENHA
        var (isValid, errorMessage) = PasswordValidator.Validate(request.NewPassword);
        if (!isValid)
            throw new InvalidOperationException(errorMessage);

        // 6.  VERIFICAR SE CONTÉM DADOS PESSOAIS
        if (PasswordValidator.ContainsPersonalInfo(request.NewPassword, user.Name.Value, user.Email.Value))
        {
            throw new InvalidOperationException("A senha não pode conter seu nome ou e-mail");
        }

        // 7.  RESETAR SENHA
        var newHash = _hasher.Hash(request.NewPassword);
        user.ChangePassword(new Domain.ValueObjects.PasswordHash(newHash));
        
        //  Resetar lockout e contador de falhas
        user.MarkLoginSuccess(now);
        
        token.MarkUsed(now);

        await _users.UpdateAsync(user, ct);
        await _tokens.UpdateAsync(token, ct);
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Senha resetada com sucesso para usuário {UserId}", user.Id);

        return Unit.Value;
    }
}
