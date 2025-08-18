using SAMGestor.Contracts;
using SAMGestor.Notification.Application.Abstractions;
using SAMGestor.Notification.Domain.Entities;
using SAMGestor.Notification.Domain.Enums;

namespace SAMGestor.Notification.Application.Orchestrators;

/// <summary>
/// Orquestra ações quando eventos chegam (ex.: participant selected) e dispara pelos canais.
/// </summary>
public class NotificationOrchestrator
{
    private readonly INotificationRepository _repo;
    private readonly IEnumerable<INotificationChannel> _channels; // email, whatsapp...
    private readonly ITemplateRenderer _renderer;
    private readonly IEventPublisher _publisher;

    public NotificationOrchestrator(
        INotificationRepository repo,
        IEnumerable<INotificationChannel> channels,
        ITemplateRenderer renderer,
        IEventPublisher publisher)
    {
        _repo = repo;
        _channels = channels;
        _renderer = renderer;
        _publisher = publisher;
    }

    public async Task OnParticipantSelectedAsync(SelectionParticipantSelectedV1 evt, CancellationToken ct)
    {
        // 1) Monta mensagem de e-mail (MVP)
        var templateSubject = "You have been selected for the retreat!";
        var templateBody = """
            Hi {{Name}},

            You have been selected for the retreat. Please complete your payment using the link below:

            {{PaymentLink}}

            Thank you!
            """;

        // OBS: No MVP, o link de pagamento será buscado via Payment API por outra etapa.
        // Aqui, apenas demonstraremos com placeholder (você troca isso quando integrar Payment).
        var variables = new Dictionary<string, string>
        {
            ["Name"] = evt.Name,
            ["PaymentLink"] = "{{PAYMENT_LINK_WILL_BE_FILLED_BY_NOTIFICATION_WHEN_CALLING_PAYMENT_API}}"
        };

        var subject = _renderer.Render(templateSubject, variables);
        var body = _renderer.Render(templateBody, variables);

        var message = new NotificationMessage(
            channel: NotificationChannel.Email,
            recipientName: evt.Name,
            recipientEmail: evt.Email,
            recipientPhone: evt.Phone,
            templateKey: "participant-selected",
            subject: subject,
            body: body,
            registrationId: evt.RegistrationId,
            retreatId: evt.RetreatId,
            externalCorrelationId: null
        );

        await _repo.AddAsync(message, ct);

        // 2) Dispara por canal de e-mail
        var emailChannel = _channels.Single(c => c.Name == "email");

        try
        {
            await emailChannel.SendAsync(message, ct);

            message.MarkSent();
            await _repo.UpdateAsync(message, ct);
            await _repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Sent, null), ct);

            await _publisher.PublishAsync(
                type: EventTypes.NotificationEmailSentV1,
                source: "sam.notification",
                data: new NotificationEmailSentV1(message.Id, evt.RegistrationId, evt.Email, DateTimeOffset.UtcNow),
                ct: ct
            );
        }
        catch (Exception ex)
        {
            message.MarkFailed(ex.Message);
            await _repo.UpdateAsync(message, ct);
            await _repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Failed, ex.Message), ct);

            await _publisher.PublishAsync(
                type: EventTypes.NotificationEmailFailedV1,
                source: "sam.notification",
                data: new NotificationEmailFailedV1(message.Id, evt.RegistrationId, evt.Email, ex.Message, DateTimeOffset.UtcNow),
                ct: ct
            );
        }
    }
}
