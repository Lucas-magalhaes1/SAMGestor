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

public sealed class PasswordChangedByAdminConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<PasswordChangedByAdminConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "notification.password.changed.by.admin";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PasswordChangedByAdminConsumer starting…");

        var exchange = string.IsNullOrWhiteSpace(opt.Exchange) ? "sam.topic" : opt.Exchange;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
                await channel.QueueBindAsync(QueueName, exchange, EventTypes.PasswordChangedByAdminV1, cancellationToken: stoppingToken);

                await channel.BasicQosAsync(0, 10, false, stoppingToken);
                logger.LogInformation("PasswordChangedByAdminConsumer listening on {queue}", QueueName);

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
                        var env = JsonSerializer.Deserialize<EventEnvelope<PasswordChangedByAdminV1>>(json, JsonOpts);

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
                        logger.LogError(ex, "Error processing password.changed.by.admin.v1. Tag={tag}", delivery.DeliveryTag);
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
                logger.LogError(ex, "PasswordChangedByAdminConsumer loop error. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("PasswordChangedByAdminConsumer stopped.");
    }

    private async Task HandleAsync(PasswordChangedByAdminV1 evt, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var renderer = scope.ServiceProvider.GetRequiredService<ITemplateRenderer>();
        var channels = scope.ServiceProvider.GetServices<INotificationChannel>().ToList();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        const string templateKey = "password-changed-by-admin";
        const string subjectTpl = "🔒 Sua senha foi alterada - SAMGestor";
        
        const string bodyTpl = """
<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="utf-8" />
    <title>Senha alterada</title>
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
              🔒 SAMGestor
            </td>
          </tr>

          <tr>
            <td style="font-size:12px; text-transform:uppercase; letter-spacing:.08em; color:#6b7280; padding-bottom:16px; text-align:center;">
              Alerta de segurança
            </td>
          </tr>

          <tr>
            <td style="font-size:18px; font-weight:600; padding-bottom:8px;">
              Olá {{Name}} 👋
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding:16px; background-color:#fef2f2; border-left:4px solid #dc2626; border-radius:6px; margin-bottom:16px;">
              A senha da sua conta no <strong>SAMGestor</strong> foi alterada pelo administrador do sistema.
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding:16px; background-color:#f9fafb; border-radius:6px; border:1px solid #e5e7eb;">
              <strong>👤 Alterado por:</strong> {{ChangedBy}}<br />
              <strong>🕒 Data e hora:</strong> {{Date}}<br />
              <strong>🔐 Sessões ativas:</strong> Todas foram encerradas por segurança
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-top:24px; padding-bottom:16px;">
              <strong>📝 Próximos passos:</strong>
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-left:16px; padding-bottom:16px;">
              1. Entre em contato com o administrador para obter sua nova senha temporária<br />
              2. Faça login com a nova senha<br />
              3. Recomendamos alterar para uma senha pessoal após o primeiro acesso
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding:16px; background-color:#fef2f2; border-left:4px solid #dc2626; border-radius:6px;">
              <strong>⚠️ Importante:</strong><br />
              Se você não solicitou essa alteração, entre em contato <strong>imediatamente</strong> com o suporte.
            </td>
          </tr>

          <tr>
            <td style="font-size:12px; line-height:1.6; color:#6b7280; padding-top:24px; padding:16px; background-color:#fffbeb; border-left:4px solid #f59e0b; border-radius:6px;">
              <strong>💡 Dica de segurança:</strong><br />
              • Nunca compartilhe sua senha com ninguém<br />
              • Use uma senha forte com letras, números e caracteres especiais<br />
              • Troque sua senha regularmente
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
            ["Name"] = evt.Name,
            ["Email"] = evt.Email,
            ["ChangedBy"] = evt.ChangedBy,
            ["Date"] = DateTimeOffset.UtcNow.ToString("dd/MM/yyyy HH:mm")
        };

        var subject = renderer.Render(subjectTpl, data);
        var body = renderer.Render(bodyTpl, data);

        var message = new NotificationMessage(
            channel: NotificationChannel.Email,
            recipientName: evt.Name,
            recipientEmail: evt.Email,
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

            logger.LogInformation("Notificação de senha alterada enviada para {Email} (UserId: {UserId})", evt.Email, evt.UserId);

            await publisher.PublishAsync(
                type: EventTypes.NotificationEmailSentV1,
                source: "sam.notification",
                data: new NotificationEmailSentV1(message.Id, Guid.Empty, evt.Email, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            message.MarkFailed(ex.Message);
            await repo.UpdateAsync(message, ct);
            await repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Failed, ex.Message), ct);

            logger.LogError(ex, "Falha ao enviar notificação para {Email}", evt.Email);

            await publisher.PublishAsync(
                type: EventTypes.NotificationEmailFailedV1,
                source: "sam.notification",
                data: new NotificationEmailFailedV1(message.Id, Guid.Empty, evt.Email, ex.Message, DateTimeOffset.UtcNow));
        }
    }
}
