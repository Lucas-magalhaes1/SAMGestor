using System.Net.Mail;
using SAMGestor.Notification.Application.Abstractions;
using SAMGestor.Notification.Domain.Entities;

namespace SAMGestor.Notification.Infrastructure.Email;

public class EmailChannel : INotificationChannel
{
    private readonly SmtpOptions _opt;

    public EmailChannel(SmtpOptions opt) => _opt = opt;

    public string Name => "email";

    public async Task SendAsync(NotificationMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.RecipientEmail))
            throw new InvalidOperationException("RecipientEmail is required for email channel.");

        using var client = new SmtpClient(_opt.Host, _opt.Port)
        {
            EnableSsl = _opt.EnableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
            UseDefaultCredentials = true // MailHog não exige auth; ajuste se usar provedor real
        };

        var from = new MailAddress(_opt.FromAddress, _opt.FromName);
        var to = new MailAddress(message.RecipientEmail!, message.RecipientName);
        using var mail = new MailMessage(from, to)
        {
            Subject = message.Subject ?? "(no subject)",
            Body = message.Body ?? string.Empty,
            IsBodyHtml = false
        };

        // Não há cancelamento no SmtpClient, então fazemos Task.Run simples
        await Task.Run(() => client.Send(mail), ct);
    }
}