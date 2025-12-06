using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Users.ForceChangeEmail;

public sealed class ForceChangeEmailHandler : IRequestHandler<ForceChangeEmailCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IEmailConfirmationTokenRepository _emailTokens;
    private readonly IOpaqueTokenGenerator _opaque;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;
    private readonly IEventBus _bus;
    private readonly ICurrentUser _currentUser;
    private readonly ILogger<ForceChangeEmailHandler> _logger;

    public ForceChangeEmailHandler(
        IUserRepository users,
        IEmailConfirmationTokenRepository emailTokens,
        IOpaqueTokenGenerator opaque,
        IDateTimeProvider clock,
        IUnitOfWork uow,
        IEventBus bus,
        ICurrentUser currentUser,
        ILogger<ForceChangeEmailHandler> logger)
    {
        _users = users;
        _emailTokens = emailTokens;
        _opaque = opaque;
        _clock = clock;
        _uow = uow;
        _bus = bus;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<Unit> Handle(ForceChangeEmailCommand request, CancellationToken ct)
    {
        // 1. Buscar usuário alvo
        var user = await _users.GetByIdAsync(request.UserId, ct);
        if (user == null)
            throw new KeyNotFoundException($"Usuário {request.UserId} não encontrado");

        // 2. Validar formato do novo e-mail
        if (!EmailAddress.IsValid(request.NewEmail))
            throw new InvalidOperationException("E-mail inválido");

        // 3. Verificar se novo e-mail já está em uso
        var existing = await _users.GetByEmailAsync(request.NewEmail, ct);
        if (existing != null && existing.Id != request.UserId)
            throw new InvalidOperationException("Este e-mail já está em uso por outro usuário");

        var oldEmail = user.Email.Value;
        var now = _clock.UtcNow;

        // 4. Atualizar e-mail e marcar como NÃO confirmado
        user.ChangeEmail(new EmailAddress(request.NewEmail));

        // 5. Gerar novo token de confirmação para o novo e-mail
        var rawToken = _opaque.GenerateSecureToken(48);
        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawToken)));

        var token = EmailConfirmationToken.Create(user.Id, tokenHash, now.AddDays(2), now);
        await _emailTokens.AddAsync(token, ct);
        await _users.UpdateAsync(user, ct);

        // 6. Publicar evento para enviar e-mail ao NOVO endereço
        var confirmUrl = $"http://localhost:3000/auth/confirm?token={rawToken}";
        
        await _bus.EnqueueAsync(
            type: EventTypes.EmailChangedByAdminV1,
            source: "sam.core",
            data: new EmailChangedByAdminV1(
                UserId: user.Id,
                Name: user.Name.Value,
                NewEmail: request.NewEmail,
                ConfirmUrl: confirmUrl
            ),
            ct: ct
        );

        // 7. Publicar evento para notificar e-mail ANTIGO
        await _bus.EnqueueAsync(
            type: EventTypes.EmailChangedNotificationV1,
            source: "sam.core",
            data: new EmailChangedNotificationV1(
                UserId: user.Id,
                OldEmail: oldEmail,
                NewEmail: request.NewEmail,
                ChangedBy: _currentUser.Email ?? "Admin"
            ),
            ct: ct
        );

        await _uow.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Admin {AdminId} alterou e-mail do usuário {UserId} de {OldEmail} para {NewEmail}",
            _currentUser.UserId, user.Id, oldEmail, request.NewEmail);

        return Unit.Value;
    }
}
