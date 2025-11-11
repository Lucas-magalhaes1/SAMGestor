namespace SAMGestor.Application.Interfaces.Auth;

public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public sealed record EmailMessage(
    string To,
    string Subject,
    string TemplateKey,
    IReadOnlyDictionary<string, string> Variables
);