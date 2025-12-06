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

public sealed class EmailChangedByAdminConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<EmailChangedByAdminConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "notification.email.changed.by.admin";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("EmailChangedByAdminConsumer starting…");

        var exchange = string.IsNullOrWhiteSpace(opt.Exchange) ? "sam.topic" : opt.Exchange;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
                await channel.QueueBindAsync(QueueName, exchange, EventTypes.EmailChangedByAdminV1, cancellationToken: stoppingToken);

                await channel.BasicQosAsync(0, 10, false, stoppingToken);
                logger.LogInformation("EmailChangedByAdminConsumer listening on {queue}", QueueName);

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
                        var env = JsonSerializer.Deserialize<EventEnvelope<EmailChangedByAdminV1>>(json, JsonOpts);

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
                        logger.LogError(ex, "Error processing email.changed.by.admin.v1. Tag={tag}", delivery.DeliveryTag);
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
                logger.LogError(ex, "EmailChangedByAdminConsumer loop error. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("EmailChangedByAdminConsumer stopped.");
    }

    private async Task HandleAsync(EmailChangedByAdminV1 evt, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var renderer = scope.ServiceProvider.GetRequiredService<ITemplateRenderer>();
        var channels = scope.ServiceProvider.GetServices<INotificationChannel>().ToList();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        const string templateKey = "email-changed-by-admin";
        const string subjectTpl = "Confirme seu novo e-mail - SAMGestor";
        
        const string bodyTpl = """
<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="utf-8" />
    <title>Confirme seu novo e-mail</title>
</head>
<body style="margin:0; padding:0; background-color:#f4f4f5;">
  <table role="presentation" width="100%" cellspacing="0" cellpadding="0" style="background-color:#f4f4f5; padding:24px 0;">
    <tr>
      <td align="center">
        <table role="presentation" width="600" cellspacing="0" cellpadding="0"
               style="background-color:#ffffff; border-radius:8px; padding:32px;
                      font-family:system-ui,-apple-system,BlinkMacSystemFont,'Segoe UI',sans-serif; color:#111827;">
          
          <tr>
            <td style="font-size:24px; font-weight:700; color:#2563eb; padding-bottom:8px; text-align:center;">
              🔄 SAMGestor
            </td>
          </tr>

          <tr>
            <td style="font-size:12px; text-transform:uppercase; letter-spacing:.08em; color:#6b7280; padding-bottom:16px; text-align:center;">
              Alteração de e-mail
            </td>
          </tr>

          <tr>
            <td style="font-size:18px; font-weight:600; padding-bottom:8px;">
              Olá {{Name}} 👋
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-bottom:16px;">
              O administrador do sistema alterou o e-mail da sua conta no <strong>SAMGestor</strong> para <strong>{{NewEmail}}</strong>.
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-bottom:16px;">
              Para confirmar o novo e-mail e continuar usando o sistema, clique no botão abaixo:
            </td>
          </tr>

          <tr>
            <td align="center" style="padding:24px 0;">
              <a href="{{ConfirmUrl}}"
                 style="display:inline-block; padding:14px 32px; border-radius:6px;
                        background-color:#2563eb; color:#ffffff !important; text-decoration:none;
                        font-size:15px; font-weight:600; box-shadow:0 4px 6px rgba(37,99,235,.25);">
                Confirmar Novo E-mail
              </a>
            </td>
          </tr>

          <tr>
            <td style="font-size:13px; line-height:1.6; color:#6b7280; padding:16px; background-color:#f9fafb; border-radius:6px; border:1px solid #e5e7eb;">
              <strong>📧 Novo e-mail:</strong> {{NewEmail}}<br />
              <strong>⏰ Este link expira em:</strong> 48 horas<br />
              <strong>🔑 Você precisará definir uma nova senha</strong> ao confirmar o e-mail
            </td>
          </tr>

          <tr>
            <td style="font-size:12px; line-height:1.5; color:#9ca3af; padding-top:24px;">
              Se o botão não funcionar, copie e cole este link no navegador:<br />
              <a href="{{ConfirmUrl}}" style="color:#2563eb; word-break:break-all;">{{ConfirmUrl}}</a>
            </td>
          </tr>

          <tr>
            <td style="font-size:12px; line-height:1.6; color:#9ca3af; padding-top:24px; border-top:1px solid #e5e7eb; text-align:center;">
              Se você não solicitou essa alteração, entre em contato com o suporte.<br />
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
            ["Name"] = evt.Name,
            ["NewEmail"] = evt.NewEmail,
            ["ConfirmUrl"] = evt.ConfirmUrl
        };

        var subject = renderer.Render(subjectTpl, data);
        var body = renderer.Render(bodyTpl, data);

        var message = new NotificationMessage(
            channel: NotificationChannel.Email,
            recipientName: evt.Name,
            recipientEmail: evt.NewEmail,
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

            logger.LogInformation("E-mail de confirmação enviado para {Email} (UserId: {UserId})", evt.NewEmail, evt.UserId);

            await publisher.PublishAsync(
                type: EventTypes.NotificationEmailSentV1,
                source: "sam.notification",
                data: new NotificationEmailSentV1(message.Id, Guid.Empty, evt.NewEmail, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            message.MarkFailed(ex.Message);
            await repo.UpdateAsync(message, ct);
            await repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Failed, ex.Message), ct);

            logger.LogError(ex, "Falha ao enviar e-mail de confirmação para {Email}", evt.NewEmail);

            await publisher.PublishAsync(
                type: EventTypes.NotificationEmailFailedV1,
                source: "sam.notification",
                data: new NotificationEmailFailedV1(message.Id, Guid.Empty, evt.NewEmail, ex.Message, DateTimeOffset.UtcNow));
        }
    }
}
