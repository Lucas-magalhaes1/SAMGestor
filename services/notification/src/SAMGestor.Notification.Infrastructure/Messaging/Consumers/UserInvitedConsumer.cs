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

public sealed class UserInvitedConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<UserInvitedConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "notification.user.invited";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("UserInvitedConsumer starting…");

        var exchange = string.IsNullOrWhiteSpace(opt.Exchange) ? "sam.topic" : opt.Exchange;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
                await channel.QueueBindAsync(QueueName, exchange, EventTypes.UserInvitedV1, cancellationToken: stoppingToken);

                await channel.BasicQosAsync(0, 10, false, stoppingToken);
                logger.LogInformation("UserInvitedConsumer listening on {queue}", QueueName);

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
                        var env = JsonSerializer.Deserialize<EventEnvelope<UserInvitedV1>>(json, JsonOpts);

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
                        logger.LogError(ex, "Error processing user.invited.v1. Tag={tag}", delivery.DeliveryTag);
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
                logger.LogError(ex, "UserInvitedConsumer loop error. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("UserInvitedConsumer stopped.");
    }

    private async Task HandleAsync(UserInvitedV1 evt, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var repo = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var renderer = scope.ServiceProvider.GetRequiredService<ITemplateRenderer>();
        var channels = scope.ServiceProvider.GetServices<INotificationChannel>().ToList();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        const string templateKey = "user-invite";
        const string subjectTpl = "Bem-vindo ao SAMGestor - Configure sua conta";
        
        const string bodyTpl = """
<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="utf-8" />
    <title>Bem-vindo ao SAMGestor</title>
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
              SAMGestor
            </td>
          </tr>

          <tr>
            <td style="font-size:12px; text-transform:uppercase; letter-spacing:.08em; color:#6b7280; padding-bottom:16px; text-align:center;">
              Convite para acessar o sistema
            </td>
          </tr>

          <tr>
            <td style="font-size:18px; font-weight:600; padding-bottom:8px;">
              Olá {{Name}} 👋
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-bottom:16px;">
              Você foi convidado por <strong>{{CreatedBy}}</strong> para acessar o <strong>SAMGestor</strong> 
              com o perfil de <strong>{{Role}}</strong>.
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-bottom:16px;">
              Para ativar sua conta e definir sua senha, clique no botão abaixo:
            </td>
          </tr>

          <tr>
            <td align="center" style="padding:24px 0;">
              <a href="{{ConfirmUrl}}"
                 style="display:inline-block; padding:14px 32px; border-radius:6px;
                        background-color:#2563eb; color:#ffffff !important; text-decoration:none;
                        font-size:15px; font-weight:600; box-shadow:0 4px 6px rgba(37,99,235,.25);">
                Configurar Minha Conta
              </a>
            </td>
          </tr>

          <tr>
            <td style="font-size:13px; line-height:1.6; color:#6b7280; padding:16px; background-color:#f9fafb; border-radius:6px; border:1px solid #e5e7eb;">
              <strong>📧 Seu login será:</strong> {{Email}}<br />
              <strong>🔑 Defina sua senha:</strong> ao clicar no botão acima<br />
              <strong>⏰ Válido por:</strong> 48 horas
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
              Este é um e-mail automático. Por favor, não responda.<br />
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
            ["Role"] = evt.Role,
            ["CreatedBy"] = evt.CreatedBy,
            ["ConfirmUrl"] = evt.ConfirmUrl
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

            logger.LogInformation("Convite enviado para {Email} (UserId: {UserId})", evt.Email, evt.UserId);

            await publisher.PublishAsync(
                type: EventTypes.NotificationEmailSentV1,
                source: "sam.notification",
                data: new NotificationEmailSentV1(message.Id, null, evt.Email, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            message.MarkFailed(ex.Message);
            await repo.UpdateAsync(message, ct);
            await repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Failed, ex.Message), ct);

            logger.LogError(ex, "Falha ao enviar convite para {Email}", evt.Email);

            await publisher.PublishAsync(
                type: EventTypes.NotificationEmailFailedV1,
                source: "sam.notification",
                data: new NotificationEmailFailedV1(message.Id, null, evt.Email, ex.Message, DateTimeOffset.UtcNow));
        }
    }
}
