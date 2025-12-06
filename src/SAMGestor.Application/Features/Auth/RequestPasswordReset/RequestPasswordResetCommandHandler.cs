using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Auth.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IOpaqueTokenGenerator _opaque;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;
    private readonly IEventBus _bus;
    private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

    public RequestPasswordResetCommandHandler(
        IUserRepository users,
        IPasswordResetTokenRepository tokens,
        IOpaqueTokenGenerator opaque,
        IDateTimeProvider clock,
        IUnitOfWork uow,
        IEventBus bus,
        ILogger<RequestPasswordResetCommandHandler> logger)
    {
        _users = users;
        _tokens = tokens;
        _opaque = opaque;
        _clock = clock;
        _uow = uow;
        _bus = bus;
        _logger = logger;
    }

    public async Task<Unit> Handle(RequestPasswordResetCommand request, CancellationToken ct)
    {
        // Sempre retorna sucesso para não vazar se e-mail existe
        var user = await _users.GetByEmailAsync(request.Email, ct);
        
        if (user is null)
        {
            _logger.LogWarning("Solicitação de reset para e-mail inexistente: {Email}", request.Email);
            // Delay proposital para dificultar enumeração de e-mails
            await Task.Delay(Random.Shared.Next(200, 500), ct);
            return Unit.Value;
        }

        // Verificar se usuário está habilitado
        if (!user.Enabled)
        {
            _logger.LogWarning("Solicitação de reset para conta desabilitada: {UserId}", user.Id);
            // Não informa que conta está desabilitada 
            await Task.Delay(Random.Shared.Next(200, 500), ct);
            return Unit.Value;
        }

        // Verificar se está em lockout
        var now = _clock.UtcNow;
        if (user.IsLocked(now))
        {
            _logger.LogWarning("Solicitação de reset para conta bloqueada: {UserId}", user.Id);
            // Não informa que está em lockout 
            await Task.Delay(Random.Shared.Next(200, 500), ct);
            return Unit.Value;
        }

        // Gerar token de reset (validade: 30 minutos - mais curto que convite)
        var rawToken = _opaque.GenerateSecureToken(48);
        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawToken)));

        var token = PasswordResetToken.Create(user.Id, tokenHash, now.AddMinutes(30), now);
        await _tokens.AddAsync(token, ct);

        // PUBLICAR EVENTO VIA OUTBOX
        var resetUrl = $"{request.ResetUrlBase.TrimEnd('/')}/auth/reset?token={rawToken}";
        
        var evt = new PasswordResetRequestedV1(
            UserId: user.Id,
            Name: user.Name.Value,
            Email: user.Email.Value,
            ResetToken: rawToken,
            ResetUrl: resetUrl,
            ExpiresAt: token.ExpiresAt
        );

        await _bus.EnqueueAsync(
            type: EventTypes.PasswordResetRequestedV1,
            source: "sam.core",
            data: evt,
            ct: ct
        );

        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation("Solicitação de reset de senha enfileirada para {UserId}", user.Id);

        return Unit.Value;
    }
}
