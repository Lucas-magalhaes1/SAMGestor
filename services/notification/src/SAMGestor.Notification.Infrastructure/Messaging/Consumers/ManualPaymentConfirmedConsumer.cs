// SAMGestor.Notification.Infrastructure/Messaging/Consumers/ManualPaymentConfirmedConsumer.cs
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAMGestor.Contracts;
using SAMGestor.Notification.Application.Abstractions;
using SAMGestor.Notification.Domain.Entities;
using SAMGestor.Notification.Domain.Enums;

namespace SAMGestor.Notification.Infrastructure.Messaging.Consumers;

public sealed class ManualPaymentConfirmedConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<ManualPaymentConfirmedConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "notification.manual-payment";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("ManualPaymentConfirmedConsumer starting…");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.ExchangeDeclareAsync(opt.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
                await channel.QueueBindAsync(QueueName, opt.Exchange, EventTypes.ManualPaymentConfirmedV1, cancellationToken: stoppingToken);

                await channel.BasicQosAsync(0, 10, false, stoppingToken);
                logger.LogInformation("ManualPaymentConfirmedConsumer listening on {queue}", QueueName);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delivery = await channel.BasicGetAsync(QueueName, autoAck: false, cancellationToken: stoppingToken);
                    if (delivery is null)
                    {
                        await Task.Delay(400, stoppingToken);
                        continue;
                    }

                    try
                    {
                        var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
                        var env = JsonSerializer.Deserialize<EventEnvelope<ManualPaymentConfirmedV1>>(json, JsonOpts);

                        if (env?.Data is null)
                        {
                            logger.LogWarning("Invalid envelope. Tag={tag}", delivery.DeliveryTag);
                            await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                            continue;
                        }

                        await HandleAsync(env.Data, stoppingToken);
                        await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing manual payment confirmed. Tag={tag}", delivery.DeliveryTag);
                        await channel.BasicNackAsync(delivery.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
                    }
                }
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                logger.LogWarning(ex, "RabbitMQ indisponível. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "ManualPaymentConfirmedConsumer loop error. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        logger.LogInformation("ManualPaymentConfirmedConsumer stopped.");
    }

    private async Task HandleAsync(ManualPaymentConfirmedV1 evt, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var renderer = scope.ServiceProvider.GetRequiredService<ITemplateRenderer>();
        var channels = scope.ServiceProvider.GetRequiredService<IEnumerable<INotificationChannel>>();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        // ✅ REMOVIDO: Dependência de selected_registrations
        // Agora usa apenas os dados do evento!

        string templateKey = "manual-payment-confirmed";
        string subjectTpl = "Pagamento confirmado pela coordenação ✅";
        string bodyTpl = """
<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="utf-8" />
    <title>Pagamento confirmado</title>
</head>
<body style="margin:0; padding:0; background-color:#f4f4f5;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f4f5; padding:24px 0;">
    <tr>
      <td align="center">
        <table role="presentation" width="600" cellspacing="0" cellpadding="0"
               style="background-color:#ffffff; border-radius:8px; padding:24px;
                      font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif; color:#111827;">

          <tr>
            <td style="font-size:12px; text-transform:uppercase; letter-spacing:.08em; color:#6b7280; padding-bottom:4px;">
              Retiro · Confirmação de Inscrição
            </td>
          </tr>

          <tr>
            <td style="font-size:20px; font-weight:600; padding-bottom:8px;">
              Olá {{Name}} 👋
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-bottom:16px;">
              Ótima notícia! A coordenação confirmou o recebimento do seu pagamento e sua inscrição para o retiro está confirmada! 🙌
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding:12px 16px; background-color:#f0fdf4;
                       border-radius:6px; border:1px solid #86efac; margin-bottom:16px;">
              <strong>Valor:</strong> {{Amount}} {{Currency}}<br />
              <strong>Método de pagamento:</strong> {{Method}}<br />
              <strong>Data de pagamento:</strong> {{PaidAt}}<br />
              <strong>Confirmado por:</strong> {{RegisteredBy}}
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-bottom:16px;">
              Este pagamento foi registrado manualmente pela coordenação do retiro.
              Faltando alguns dias para o evento, enviaremos um novo e-mail com instruções práticas,
              horários e o que levar.
            </td>
          </tr>

          <tr>
            <td style="font-size:12px; line-height:1.6; color:#9ca3af; padding-top:16px; border-top:1px solid #e5e7eb;">
              Se tiver qualquer dúvida, é só responder esta mensagem.<br />
              <span style="color:#6b7280;">Equipe de coordenação do retiro</span>
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>
""";

        var data = new Dictionary<string, string>
        {
            ["Name"] = evt.Name,                            // ← Do evento
            ["Amount"] = evt.Amount.ToString("0.00"),       // ← Do evento
            ["Currency"] = evt.Currency,                    // ← Do evento
            ["Method"] = evt.PaymentMethod,                 // ← Do evento
            ["PaidAt"] = evt.PaymentDate.ToString("dd/MM/yyyy"),
            ["RegisteredBy"] = evt.RegisteredBy             // ← Do evento
        };

        var subject = renderer.Render(subjectTpl, data);
        var body = renderer.Render(bodyTpl, data);

        var message = new NotificationMessage(
            channel: NotificationChannel.Email,
            recipientName: evt.Name,         // ← Do evento
            recipientEmail: evt.Email,       // ← Do evento
            recipientPhone: null,
            templateKey: templateKey,
            subject: subject,
            body: body,
            registrationId: evt.RegistrationId,
            retreatId: evt.RetreatId,
            externalCorrelationId: evt.ProofId.ToString()
        );

        await repo.AddAsync(message, ct);

        var emailChannel = channels.Single(c => c.Name == "email");

        try
        {
            await emailChannel.SendAsync(message, ct);

            message.MarkSent();
            await repo.UpdateAsync(message, ct);
            await repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Sent, null), ct);

            await publisher.PublishAsync(
                type: EventTypes.NotificationEmailSentV1,
                source: "sam.notification",
                data: new NotificationEmailSentV1(message.Id, evt.RegistrationId, evt.Email, DateTimeOffset.UtcNow));
            
            logger.LogInformation(
                "Email de pagamento manual enviado: {RegistrationId} → {Email}",
                evt.RegistrationId, evt.Email);
        }
        catch (Exception ex)
        {
            message.MarkFailed(ex.Message);
            await repo.UpdateAsync(message, ct);
            await repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Failed, ex.Message), ct);

            await publisher.PublishAsync(
                type: EventTypes.NotificationEmailFailedV1,
                source: "sam.notification",
                data: new NotificationEmailFailedV1(message.Id, evt.RegistrationId, evt.Email, ex.Message, DateTimeOffset.UtcNow));
            
            logger.LogError(ex, 
                "Falha ao enviar email de pagamento manual: {RegistrationId}",
                evt.RegistrationId);
        }
    }
}
