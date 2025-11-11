using MediatR;
using SAMGestor.Application.Dtos.Users;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth; 
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Users.Create;

public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResponse>
{
    private readonly IUserRepository _users;
    private readonly IEmailConfirmationTokenRepository _emailTokens;
    private readonly IOpaqueTokenGenerator _opaque;
    private readonly IEmailSender _email;
    private readonly IUnitOfWork _uow;
    private readonly IDateTimeProvider _clock;

    private readonly IPasswordHasher _hasher;      

    public CreateUserCommandHandler(
        IUserRepository users,
        IEmailConfirmationTokenRepository emailTokens,
        IOpaqueTokenGenerator opaque,
        IEmailSender email,
        IUnitOfWork uow,
        IDateTimeProvider clock,
        IPasswordHasher hasher)                   
    {
        _users = users;
        _emailTokens = emailTokens;
        _opaque = opaque;
        _email = email;
        _uow = uow;
        _clock = clock;
        _hasher = hasher;                          
    }

    public async Task<CreateUserResponse> Handle(CreateUserCommand request, CancellationToken ct)
    {
        var role = request.Role?.Trim().ToLowerInvariant() switch
        {
            "admin" or "administrator" => UserRole.Administrator,
            "manager"                  => UserRole.Manager,
            _                          => UserRole.Consultant
        };

        // senha temporária aleatória (o user trocará ao confirmar e-mail)
        var tempPass = _opaque.GenerateSecureToken(16);
        var hashed   = _hasher.Hash(tempPass);             
        var user = new User(
            new FullName(request.Name),
            new EmailAddress(request.Email),
            request.Phone,
            new PasswordHash(hashed),                      
            role);

        await _users.AddAsync(user, ct);

        // token de confirmação
        var now = _clock.UtcNow;
        var raw = _opaque.GenerateSecureToken(48);
        var sha = Convert.ToBase64String(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(raw)));

        var token = EmailConfirmationToken.Create(user.Id, sha, now.AddDays(2), now);
        await _emailTokens.AddAsync(token, ct);
        await _uow.SaveChangesAsync(ct);

        var confirmUrl = $"{request.ConfirmUrlBase.TrimEnd('/')}/auth/confirm?token={raw}";
        var msg = new EmailMessage(
            To: user.Email.Value,
            Subject: "Convite de acesso",
            TemplateKey: "INVITE_USER",
            Variables: new Dictionary<string, string>
            {
                ["name"] = user.Name.Value,
                ["confirmUrl"] = confirmUrl
            }
        );
        await _email.SendAsync(msg, ct);

        return new CreateUserResponse(user.Id);
    }
}
