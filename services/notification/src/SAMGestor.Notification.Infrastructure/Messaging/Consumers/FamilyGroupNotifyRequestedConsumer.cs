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

public sealed class FamilyGroupNotifyRequestedConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<FamilyGroupNotifyRequestedConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "notification.familygroups.notify";
    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FamilyGroupNotifyRequestedConsumer starting…");
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
                await ch.QueueBindAsync(QueueName, exchange, EventTypes.FamilyGroupNotifyRequestedV1, cancellationToken: stoppingToken);
                await ch.BasicQosAsync(0, 10, false, stoppingToken);
                logger.LogInformation("FamilyGroupNotifyRequestedConsumer listening on {q}", QueueName);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delivery = await ch.BasicGetAsync(QueueName, autoAck: false, cancellationToken: stoppingToken);
                    if (delivery is null) { await Task.Delay(400, stoppingToken); continue; }

                    try
                    {
                        var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
                        var env  = JsonSerializer.Deserialize<EventEnvelope<FamilyGroupNotifyRequestedV1>>(json, Json);
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
                        logger.LogError(ex, "Error processing family.group.notify.requested.v1");
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
                logger.LogError(ex, "NotifyRequestedConsumer loop error. Retry 2s…");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }

    private async Task HandleAsync(FamilyGroupNotifyRequestedV1 req, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var repo        = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var renderer    = scope.ServiceProvider.GetRequiredService<ITemplateRenderer>();
        var channels    = scope.ServiceProvider.GetServices<INotificationChannel>().ToList();

        var emailChannel    = channels.FirstOrDefault(c => c.Name == "email");
        var whatsappChannel = channels.FirstOrDefault(c => c.Name == "whatsapp"); 

        const string subjectTpl = "Reenvio: Grupo da sua família no retiro";
        const string bodyTpl = """
            Olá {{Name}},

            Este é o link do grupo da sua família: {{GroupLink}}

            Se já está no grupo, ignore esta mensagem.
            """;

        var seenEmails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var seenPhones = new HashSet<string>(StringComparer.Ordinal);

        foreach (var m in req.Members)
        {
            if (emailChannel is not null && !string.IsNullOrWhiteSpace(m.Email) && seenEmails.Add(m.Email!))
            {
                var data    = new Dictionary<string,string> { ["Name"] = m.Name, ["GroupLink"] = req.GroupLink };
                var subject = renderer.Render(subjectTpl, data);
                var body    = renderer.Render(bodyTpl,   data);

                var msg = new NotificationMessage(
                    channel: NotificationChannel.Email,
                    recipientName: m.Name,
                    recipientEmail: m.Email!,
                    recipientPhone: m.PhoneE164,
                    templateKey: "family-group-link-resend",
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
                    templateKey: "family-group-link-resend",
                    subject: "Link do grupo da sua família",
                    body: $"Olá {m.Name}, este é o link do grupo da sua família: {req.GroupLink}",
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
    }
}
