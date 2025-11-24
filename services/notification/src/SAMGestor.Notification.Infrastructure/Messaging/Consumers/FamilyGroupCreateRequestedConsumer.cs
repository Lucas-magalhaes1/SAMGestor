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

public sealed class FamilyGroupCreateRequestedConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<FamilyGroupCreateRequestedConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "notification.familygroups";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FamilyGroupCreateRequestedConsumer starting…");
        var exchange = string.IsNullOrWhiteSpace(opt.Exchange) ? "sam.topic" : opt.Exchange;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var ch = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await ch.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await ch.QueueDeclareAsync(
                    QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    noWait: false,
                    cancellationToken: stoppingToken
                );
                await ch.QueueBindAsync(QueueName, exchange, EventTypes.FamilyGroupCreateRequestedV1, cancellationToken: stoppingToken);
                await ch.BasicQosAsync(0, 10, false, stoppingToken);
                logger.LogInformation("FamilyGroupCreateRequestedConsumer listening on {queue}", QueueName);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delivery = await ch.BasicGetAsync(QueueName, autoAck: false, cancellationToken: stoppingToken);
                    if (delivery is null) { await Task.Delay(400, stoppingToken); continue; }

                    try
                    {
                        var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
                        var env  = JsonSerializer.Deserialize<EventEnvelope<FamilyGroupCreateRequestedV1>>(json, Json);
                        if (env?.Data is null)
                        {
                            logger.LogWarning("Invalid envelope. Tag={tag}", delivery.DeliveryTag);
                            await ch.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                            continue;
                        }

                        await HandleAsync(env.Data, stoppingToken);
                        await ch.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing family.group.create.requested.v1");
                        await ch.BasicNackAsync(delivery.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
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
                logger.LogError(ex, "FamilyGroupCreateRequestedConsumer loop error. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task HandleAsync(FamilyGroupCreateRequestedV1 req, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var repo        = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var renderer    = scope.ServiceProvider.GetRequiredService<ITemplateRenderer>();
        var channels    = scope.ServiceProvider.GetServices<INotificationChannel>().ToList();
        var publisher   = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        var emailChannel    = channels.FirstOrDefault(c => c.Name == "email");
        var whatsappChannel = channels.FirstOrDefault(c => c.Name == "whatsapp");

        
        var inviteLink = $"https://groups.example/{req.FamilyId}/{Guid.NewGuid():N}";
        string? externalId = null;
        
        const string subjectTpl = "Grupo da sua família no retiro";
const string bodyTpl = """
<!DOCTYPE html>
<html lang="pt-BR">
<head>
    <meta charset="utf-8" />
    <title>Grupo da sua família no retiro</title>
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
              Retiro · Grupo da Família
            </td>
          </tr>

          <tr>
            <td style="font-size:20px; font-weight:600; padding-bottom:8px;">
              Olá {{Name}} 👋
            </td>
          </tr>

          <tr>
            <td style="font-size:14px; line-height:1.6; padding-bottom:16px;">
              Criamos um grupo da sua família para facilitar a comunicação antes e durante o retiro.
              Acesse pelo link abaixo:
            </td>
          </tr>

          <tr>
            <td align="center" style="padding:20px 0;">
              <a href="{{GroupLink}}"
                 style="display:inline-block; padding:12px 28px; border-radius:6px;
                        background-color:#2563eb; color:#ffffff; text-decoration:none;
                        font-size:14px; font-weight:600;">
                Entrar no grupo da minha família
              </a>
            </td>
          </tr>

          <tr>
            <td style="font-size:13px; line-height:1.6; padding-bottom:16px;">
              Se o botão acima não funcionar, copie e cole este link no seu navegador ou diretamente no WhatsApp:<br />
              <span style="font-size:12px; color:#2563eb;">{{GroupLink}}</span>
            </td>
          </tr>

          <tr>
            <td style="font-size:12px; line-height:1.6; color:#9ca3af; padding-top:16px; border-top:1px solid #e5e7eb;">
              Qualquer dúvida, fale com a coordenação respondendo este e-mail.<br />
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

        var seenEmails  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenPhones  = new HashSet<string>(StringComparer.Ordinal);

        foreach (var m in req.Members)
        {
            if (emailChannel is not null && !string.IsNullOrWhiteSpace(m.Email) && seenEmails.Add(m.Email!))
            {
                var data    = new Dictionary<string,string> { ["Name"] = m.Name, ["GroupLink"] = inviteLink };
                var subject = renderer.Render(subjectTpl, data);
                var body    = renderer.Render(bodyTpl,   data);

                var msg = new NotificationMessage(
                    channel: NotificationChannel.Email,
                    recipientName: m.Name,
                    recipientEmail: m.Email!,
                    recipientPhone: m.PhoneE164,
                    templateKey: "family-group-link",
                    subject: subject,
                    body: body,
                    registrationId: m.RegistrationId,
                    retreatId: req.RetreatId,
                    externalCorrelationId: req.FamilyId.ToString()
                );

                await repo.AddAsync(msg, ct);
                try
                {
                    await emailChannel.SendAsync(msg, ct);
                    msg.MarkSent();
                    await repo.UpdateAsync(msg, ct);
                    await repo.AddLogAsync(new NotificationDispatchLog(msg.Id, NotificationStatus.Sent, null), ct);
                }
                catch (Exception ex)
                {
                    msg.MarkFailed(ex.Message);
                    await repo.UpdateAsync(msg, ct);
                    await repo.AddLogAsync(new NotificationDispatchLog(msg.Id, NotificationStatus.Failed, ex.Message), ct);
                }
            }
            
            if (whatsappChannel is not null && !string.IsNullOrWhiteSpace(m.PhoneE164) && seenPhones.Add(m.PhoneE164!))
            {
                var msg = new NotificationMessage(
                    channel: NotificationChannel.WhatsApp,
                    recipientName: m.Name,
                    recipientEmail: null,
                    recipientPhone: m.PhoneE164!,
                    templateKey: "family-group-link",
                    subject: "Link do grupo da sua família",
                    body: $"Olá {m.Name}, este é o link do grupo da sua família: {inviteLink}",
                    registrationId: m.RegistrationId,
                    retreatId: req.RetreatId,
                    externalCorrelationId: req.FamilyId.ToString()
                );

                await repo.AddAsync(msg, ct);
                try
                {
                    await whatsappChannel.SendAsync(msg, ct);
                    msg.MarkSent();
                    await repo.UpdateAsync(msg, ct);
                    await repo.AddLogAsync(new NotificationDispatchLog(msg.Id, NotificationStatus.Sent, null), ct);
                }
                catch (Exception ex)
                {
                    msg.MarkFailed(ex.Message);
                    await repo.UpdateAsync(msg, ct);
                    await repo.AddLogAsync(new NotificationDispatchLog(msg.Id, NotificationStatus.Failed, ex.Message), ct);
                }
            }
        }
        
        await publisher.PublishAsync(
            type: EventTypes.FamilyGroupCreatedV1,
            source: "sam.notification",
            data: new FamilyGroupCreatedV1(
                RetreatId: req.RetreatId,
                FamilyId:  req.FamilyId,
                Channel:   "whatsapp",   
                Link:      inviteLink,
                ExternalId: externalId,
                CreatedAt: DateTimeOffset.UtcNow
            ),
            traceId: null,
            ct: ct
        );
    }
}
