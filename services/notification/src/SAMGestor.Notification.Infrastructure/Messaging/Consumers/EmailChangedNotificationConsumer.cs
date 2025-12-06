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

public sealed class EmailChangedNotificationConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<EmailChangedNotificationConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "notification.email.changed.notification";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EmailChangedNotificationConsumer starting…");

        var exchange = string.IsNullOrWhiteSpace(opt.Exchange) ? "sam.topic" : opt.Exchange;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
                await channel.QueueBindAsync(QueueName, exchange, EventTypes.EmailChangedNotificationV1, cancellationToken: stoppingToken);

                await channel.BasicQosAsync(0, 10, false, stoppingToken);
                logger.LogInformation("EmailChangedNotificationConsumer listening on {queue}", QueueName);

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
                        var env = JsonSerializer.Deserialize<EventEnvelope<EmailChangedNotificationV1>>(json, JsonOpts);

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
                        logger.LogError(ex, "Error processing email.changed.notification.v1. Tag={tag}", delivery.DeliveryTag);
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
                logger.LogError(ex, "EmailChangedNotificationConsumer loop error. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("EmailChangedNotificationConsumer stopped.");
    }

    private async Task HandleAsync(EmailChangedNotificationV1 evt, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var renderer = scope.ServiceProvider.GetRequiredService<ITemplateRenderer>();
        var channels = scope.ServiceProvider.GetServices<INotificationChannel>().ToList();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        const string templateKey = "email-changed-notification";
        const string subjectTpl = "⚠️ Alteração de e-mail na sua conta - SAMGestor";
        
        const string bodyTpl = """
<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="utf-8" />
    <title>Alteração de e-mail detectada</title>
</head>
<body style="margin:0; padding:0; background-color:#f4f4f5;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f4f5; padding:24px 0;">
    <tr>
      <td align="center">
        <table role="presentation" width="600" cellspacing="0" cellpadding="0"
               style="background-color:#ffffff; border-radius:8px; padding:32px;
                      font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif; color:#111827;">
          
          <tr>
            <td style="font-size:24px; font-weight:700; color:#dc2626; padding-bottom:8px; text-align:center;">
              ⚠️ SAMGestor
            </td>
          </tr>

          <tr>
            <td style="font-size:12px; text-transform:uppercase; letter-spacing:.08em; color:#6b7280; padding-bottom:16px; text-align:center;">
              Alerta de segurança
            </td>
          </tr>

          <tr>
            <td style="font-size:18px; font-weight:600; padding-bottom:8px;">
              Alteração de E-mail Detectada
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding:16px; background-color:#fef2f2; border-left:4px solid #dc2626; border-radius:6px; margin-bottom:16px;">
              O e-mail da sua conta no <strong>SAMGestor</strong> foi alterado pelo administrador do sistema.
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding:16px; background-color:#f9fafb; border-radius:6px; border:1px solid #e5e7eb;">
              <strong>📧 E-mail anterior:</strong> {{OldEmail}}<br />
              <strong>📧 Novo e-mail:</strong> {{NewEmail}}<br />
              <strong>👤 Alterado por:</strong> {{ChangedBy}}<br />
              <strong>🕒 Data:</strong> {{Date}}
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-top:24px; padding-bottom:16px;">
              <strong>⚠️ Ação necessária:</strong><br />
              Se você <strong>não solicitou</strong> essa alteração, entre em contato <strong>imediatamente</strong> com o suporte.
            </td>
          </tr>

          <tr>
            <td style="font-size:12px; line-height:1.6; color:#6b7280; padding:16px; background-color:#fffbeb; border-left:4px solid #f59e0b; border-radius:6px;">
              <strong>💡 Dica de segurança:</strong><br />
              Esta notificação foi enviada para o e-mail antigo para garantir que você esteja ciente de qualquer alteração na sua conta.
            </td>
          </tr>

          <tr>
            <td style="font-size:12px; line-height:1.6; color:#9ca3af; padding-top:24px; border-top:1px solid #e5e7eb; text-align:center;">
              Este é um e-mail automático de segurança. Por favor, não responda.<br />
              <span style="color:#6b7280;">&copy; 2025 SAMGestor. Todos os direitos reservados.</span>
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
            ["OldEmail"] = evt.OldEmail,
            ["NewEmail"] = evt.NewEmail,
            ["ChangedBy"] = evt.ChangedBy,
            ["Date"] = DateTimeOffset.UtcNow.ToString("dd/MM/yyyy HH:mm")
        };

        var subject = renderer.Render(subjectTpl, data);
        var body = renderer.Render(bodyTpl, data);

        var message = new NotificationMessage(
            channel: NotificationChannel.Email,
            recipientName: null,
            recipientEmail: evt.OldEmail,
            recipientPhone: null,
            templateKey: templateKey,
            subject: subject,
            body: body,
            registrationId: null,
            retreatId: null,
            externalCorrelationId: evt.UserId.ToString()
        );

        await repo.AddAsync(message, ct);

        var emailChannel = channels.Single(c => c.Name == "email");

        try
        {
            await emailChannel.SendAsync(message, ct);

            message.MarkSent();
            await repo.UpdateAsync(message, ct);
            await repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Sent, null), ct);

            logger.LogInformation("Notificação de mudança de e-mail enviada para {OldEmail} (UserId: {UserId})", evt.OldEmail, evt.UserId);

            await publisher.PublishAsync(
                type: EventTypes.NotificationEmailSentV1,
                source: "sam.notification",
                data: new NotificationEmailSentV1(message.Id, Guid.Empty, evt.OldEmail, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            message.MarkFailed(ex.Message);
            await repo.UpdateAsync(message, ct);
            await repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Failed, ex.Message), ct);

            logger.LogError(ex, "Falha ao enviar notificação para {OldEmail}", evt.OldEmail);

            await publisher.PublishAsync(
                type: EventTypes.NotificationEmailFailedV1,
                source: "sam.notification",
                data: new NotificationEmailFailedV1(message.Id, Guid.Empty, evt.OldEmail, ex.Message, DateTimeOffset.UtcNow));
        }
    }
}
