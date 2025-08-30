using System.Linq;
using SAMGestor.Contracts;
using SAMGestor.Notification.Application.Abstractions;
using SAMGestor.Notification.Domain.Entities;
using SAMGestor.Notification.Domain.Enums;

namespace SAMGestor.Notification.Application.Orchestrators;

public class NotificationOrchestrator
{
    private readonly INotificationRepository _repo;
    private readonly IEnumerable<INotificationChannel> _channels;
    private readonly ITemplateRenderer _renderer;
    private readonly IEventPublisher _publisher;
    private readonly IPaymentLinkClient _payment;

    public NotificationOrchestrator(
        INotificationRepository repo,
        IEnumerable<INotificationChannel> channels,
        ITemplateRenderer renderer,
        IEventPublisher publisher,
        IPaymentLinkClient payment)
    {
        _repo = repo;
        _channels = channels;
        _renderer = renderer;
        _publisher = publisher;
        _payment = payment;
    }

    public async Task OnParticipantSelectedAsync(SelectionParticipantSelectedV1 evt, CancellationToken cancellationToken)
    {
        // 1) gera link de pagamento (fake por enquanto)
        // Se seu IPaymentLinkClient aceitar amount/currency, prefira sobrecarga com esses dados:
        // var paymentLink = await _payment.CreatePaymentLinkAsync(evt.RegistrationId, evt.Amount, evt.Currency, cancellationToken);
        var paymentLink = await _payment.CreatePaymentLinkAsync(evt.RegistrationId, cancellationToken);

        // 2) template simples (inclui valor)
        var templateSubject = "You have been selected for the retreat!";
        var templateBody = """
            Hi {{Name}},

            You have been selected for the retreat.

            Amount due: {{Amount}} {{Currency}}
            Please complete your payment using the link below:

            {{PaymentLink}}

            Thank you!
            """;

        var vars = new Dictionary<string, string>
        {
            ["Name"]        = evt.Name,
            ["PaymentLink"] = paymentLink,
            ["Amount"]      = evt.Amount.ToString("0.00"),
            ["Currency"]    = evt.Currency
        };

        var subject = _renderer.Render(templateSubject, vars);
        var body    = _renderer.Render(templateBody, vars);

        var message = new NotificationMessage(
            channel:              NotificationChannel.Email,
            recipientName:        evt.Name,
            recipientEmail:       evt.Email,
            recipientPhone:       evt.Phone,
            templateKey:          "participant-selected",
            subject:              subject,
            body:                 body,
            registrationId:       evt.RegistrationId,
            retreatId:            evt.RetreatId,
            externalCorrelationId: null
        );

        await _repo.AddAsync(message, cancellationToken);

        var emailChannel = _channels.Single(c => c.Name == "email");

        try
        {
            await emailChannel.SendAsync(message, cancellationToken);

            message.MarkSent();
            await _repo.UpdateAsync(message, cancellationToken);
            await _repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Sent, null), cancellationToken);

            await _publisher.PublishAsync(
                type:   EventTypes.NotificationEmailSentV1,
                source: "sam.notification",
                data:   new NotificationEmailSentV1(message.Id, evt.RegistrationId, evt.Email, DateTimeOffset.UtcNow),
                traceId: null,
                ct : cancellationToken
            );
        }
        catch (Exception ex)
        {
            message.MarkFailed(ex.Message);
            await _repo.UpdateAsync(message, cancellationToken);
            await _repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Failed, ex.Message), cancellationToken);

            await _publisher.PublishAsync(
                type:   EventTypes.NotificationEmailFailedV1,
                source: "sam.notification",
                data:   new NotificationEmailFailedV1(message.Id, evt.RegistrationId, evt.Email, ex.Message, DateTimeOffset.UtcNow),
                traceId: null,
                ct:  cancellationToken
            );
        }
    }
}
