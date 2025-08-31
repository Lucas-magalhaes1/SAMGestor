using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAMGestor.Contracts;
using SAMGestor.Notification.Application.Abstractions;
using SAMGestor.Notification.Domain.Entities;
using SAMGestor.Notification.Domain.Enums;
using SAMGestor.Notification.Infrastructure.Persistence;

namespace SAMGestor.Notification.Infrastructure.Messaging;

public sealed class PaymentLinkCreatedConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<PaymentLinkCreatedConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "notification.payment";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("PaymentLinkCreatedConsumer starting…");

        var exchange = string.IsNullOrWhiteSpace(opt.Exchange) ? "sam.topic" : opt.Exchange;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
                await channel.QueueBindAsync(QueueName, exchange, EventTypes.PaymentLinkCreatedV1, cancellationToken: stoppingToken);

                await channel.BasicQosAsync(0, 10, false, stoppingToken);
                logger.LogInformation("PaymentLinkCreatedConsumer listening on {queue}", QueueName);

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
                        var env  = JsonSerializer.Deserialize<EventEnvelope<PaymentLinkCreatedV1>>(json, JsonOpts);

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
                        logger.LogError(ex, "Error processing link.created.v1. Tag={tag}", delivery.DeliveryTag);
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
                logger.LogError(ex, "PaymentLinkCreatedConsumer loop error. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("PaymentLinkCreatedConsumer stopped.");
    }

    private async Task HandleAsync(PaymentLinkCreatedV1 evt, CancellationToken ct)
    {
        using var scope = sp.CreateScope();
        var db        = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        var repo      = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
        var renderer  = scope.ServiceProvider.GetRequiredService<ITemplateRenderer>();
        var channels  = scope.ServiceProvider.GetServices<INotificationChannel>().ToList();
        var publisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

        // carrega a projeção
        var sel = await db.SelectedRegistrations
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.RegistrationId == evt.RegistrationId, ct);

        if (sel is null)
        {
            // Sem projeção: não envia (poderia logar/emitir alerta)
            return;
        }

        // template simples
        var subjectTpl = "Seu link de pagamento do retiro";
        var bodyTpl = """
            Olá {{Name}},

            Valor: {{Amount}} {{Currency}}
            Link de pagamento: {{LinkUrl}}

            Se já pagou, ignore este e-mail.
            """;

        var data = new Dictionary<string, string>
        {
            ["Name"]     = sel.Name,
            ["Amount"]   = evt.Amount.ToString("0.00"),
            ["Currency"] = evt.Currency,
            ["LinkUrl"]  = evt.LinkUrl
        };

        var subject = renderer.Render(subjectTpl, data);
        var body    = renderer.Render(bodyTpl, data);

        var message = new NotificationMessage(
            channel: NotificationChannel.Email,
            recipientName: sel.Name,
            recipientEmail: sel.Email,
            recipientPhone: sel.Phone,
            templateKey: "participant-payment-link",
            subject: subject,
            body: body,
            registrationId: evt.RegistrationId,
            retreatId: evt.RetreatId,
            externalCorrelationId: evt.PaymentId.ToString()
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
                data: new NotificationEmailSentV1(message.Id, evt.RegistrationId, sel.Email, DateTimeOffset.UtcNow));
        }
        catch (Exception ex)
        {
            message.MarkFailed(ex.Message);
            await repo.UpdateAsync(message, ct);
            await repo.AddLogAsync(new NotificationDispatchLog(message.Id, NotificationStatus.Failed, ex.Message), ct);

            await publisher.PublishAsync(
                type: EventTypes.NotificationEmailFailedV1,
                source: "sam.notification",
                data: new NotificationEmailFailedV1(message.Id, evt.RegistrationId, sel.Email, ex.Message, DateTimeOffset.UtcNow));
        }
    }
}
