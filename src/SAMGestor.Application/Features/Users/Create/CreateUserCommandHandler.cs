using MediatR;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Dtos.Users;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Contracts;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.Create;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IUserRepository _users;
    private readonly IEmailConfirmationTokenRepository _emailTokens;
    private readonly IOpaqueTokenGenerator _opaque;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;
    private readonly ICurrentUser _currentUser;
    private readonly IEventBus _bus; 
    private readonly ILogger<CreateUserCommandHandler> _logger;

    public CreateUserCommandHandler(
        IUserRepository users,
        IEmailConfirmationTokenRepository emailTokens,
        IOpaqueTokenGenerator opaque,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        ICurrentUser currentUser,
        IEventBus bus,
        ILogger<CreateUserCommandHandler> logger)
    {
        _users = users;
        _emailTokens = emailTokens;
        _opaque = opaque;
        _uow = uow;
        _clock = clock;
        _currentUser = currentUser;
        _bus = bus;
        _logger = logger;
    }

    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken ct)
    {
        // 1. Parse do role do novo usuário
        var newUserRole = request.Role?.Trim().ToLowerInvariant() switch
        {
            "admin" or "administrator" => UserRole.Administrator,
            "manager" => UserRole.Manager,
            _ => UserRole.Consultant
        };

        // 2.  VALIDAR HIERARQUIA DE CRIAÇÃO
        var creatorRoleStr = _currentUser.Role;
        var creatorRole = creatorRoleStr?.ToLowerInvariant() switch
        {
            "admin" or "administrator" => UserRole.Administrator,
            "manager" => UserRole.Manager,
            "consultant" => UserRole.Consultant,
            _ => throw new UnauthorizedAccessException("Usuário sem permissão para criar outros usuários")
        };

        // Consultant não pode criar ninguém
        if (creatorRole == UserRole.Consultant)
        {
            _logger.LogWarning("Consultant {UserId} tentou criar usuário", _currentUser.UserId);
            throw new ForbiddenException("Consultores não podem criar usuários");
        }

        // Manager só pode criar Consultant
        if (creatorRole == UserRole.Manager && newUserRole != UserRole.Consultant)
        {
            _logger.LogWarning(
                "Manager {UserId} tentou criar usuário com role {Role}",
                _currentUser.UserId, newUserRole);
            throw new ForbiddenException("Gestores só podem criar Consultores");
        }

        // 3. Verificar se e-mail já existe
        var existingUser = await _users.GetByEmailAsync(request.Email, ct);
        if (existingUser != null)
        {
            throw new InvalidOperationException("Já existe um usuário com este e-mail");
        }

        // 4.  CRIAR USUÁRIO SEM SENHA (será definida na confirmação)
        var placeholderHash = new PasswordHash("PENDING_CONFIRMATION");
        
        var user = new User(
            new FullName(request.Name),
            new EmailAddress(request.Email),
            request.Phone,
            placeholderHash,
            newUserRole);

        await _users.AddAsync(user, ct);

        // 5. Gerar token de confirmação (48h de validade)
        var now = _clock.UtcNow;
        var rawToken = _opaque.GenerateSecureToken(48);
        var tokenHash = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(rawToken)));

        var token = EmailConfirmationToken.Create(user.Id, tokenHash, now.AddDays(2), now);
        await _emailTokens.AddAsync(token, ct);

        // 6. PUBLICAR EVENTO VIA OUTBOX (IEventBus)
        var confirmUrl = $"{request.ConfirmUrlBase.TrimEnd('/')}/auth/confirm?token={rawToken}";
        var creatorEmail = _currentUser.Email ?? "sistema";
        
        var evt = new UserInvitedV1(
            UserId: user.Id,
            Name: user.Name.Value,
            Email: user.Email.Value,
            Role: TranslateRole(newUserRole),
            ConfirmationToken: rawToken,
            ConfirmUrl: confirmUrl,
            ExpiresAt: token.ExpiresAt,
            CreatedBy: creatorEmail
        );

        await _bus.EnqueueAsync(
            type: EventTypes.UserInvitedV1,
            source: "sam.core",
            data: evt,
            ct: ct
        );

        // 7. Commit da transação (salva usuário + token + evento no Outbox)
        await _uow.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Usuário {UserId} criado (role: {Role}) por {CreatorId}. Evento enfileirado no Outbox.",
            user.Id, newUserRole, _currentUser.UserId);

        return new CreateUserResponse(user.Id);
    }

    private static string TranslateRole(UserRole role) => role switch
    {
        UserRole.Administrator => "Administrador",
        UserRole.Manager => "Gestor",
        UserRole.Consultant => "Consultor",
        _ => "Usuário"
    };
}
