using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Application.Interfaces.Auth;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Auth.RequestPasswordReset;

public sealed class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Unit>
{
    private readonly IUserRepository _users;
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IOpaqueTokenGenerator _opaque;
    private readonly IEmailSender _email;
    private readonly IDateTimeProvider _clock;
    private readonly IUnitOfWork _uow;

    public RequestPasswordResetCommandHandler(
        IUserRepository users,
        IPasswordResetTokenRepository tokens,
        IOpaqueTokenGenerator opaque,
        IEmailSender email,
        IDateTimeProvider clock,
        IUnitOfWork uow)
    {
        _users = users;
        _tokens = tokens;
        _opaque = opaque;
        _email = email;
        _clock = clock;
        _uow = uow;
    }

    public async Task<Unit> Handle(RequestPasswordResetCommand request, CancellationToken ct)
    {
        // Resposta sempre 200 para não vazar se email existe.
        var user = await _users.GetByEmailAsync(request.Email, ct);
        if (user is null) return Unit.Value;

        var now = _clock.UtcNow;
        var raw = _opaque.GenerateSecureToken(48);
        var hash = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(raw)));

        var token = PasswordResetToken.Create(user.Id, hash, now.AddMinutes(30), now);
        await _tokens.AddAsync(token, ct);
        await _uow.SaveChangesAsync(ct);

        var url = $"{request.ResetUrlBase.TrimEnd('/')}/auth/reset?token={raw}";
        var msg = new EmailMessage(
            To: user.Email.Value,
            Subject: "Redefinição de senha",
            TemplateKey: "RESET_PASSWORD",
            Variables: new Dictionary<string, string> { ["name"] = user.Name.Value, ["resetUrl"] = url }
        );
        await _email.SendAsync(msg, ct);

        return Unit.Value;
    }
}
