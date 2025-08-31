using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAMGestor.Contracts;
using SAMGestor.Notification.Application.Abstractions;
using SAMGestor.Notification.Infrastructure.Persistence;
using SAMGestor.Notification.Domain.Entities;

namespace SAMGestor.Notification.Infrastructure.Messaging;

public sealed class SelectionEventConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<SelectionEventConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SelectionEventConsumer starting…");

        // defaults seguros caso não exista no appsettings
        var exchange   = string.IsNullOrWhiteSpace(opt.Exchange) ? "sam.topic" : opt.Exchange;
        var queueName  = string.IsNullOrWhiteSpace(opt.SelectionQueue) ? "notification.selection" : opt.SelectionQueue;
        var bindKeyCfg = string.IsNullOrWhiteSpace(opt.SelectionRoutingKey) ? EventTypes.SelectionParticipantSelectedV1 : opt.SelectionRoutingKey;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true, exclusive: false, autoDelete: false,
                    arguments: null, cancellationToken: stoppingToken);

                // binds: rota configurável + contrato v1
                await channel.QueueBindAsync(queue: queueName, exchange: exchange, routingKey: bindKeyCfg, cancellationToken: stoppingToken);
                await channel.QueueBindAsync(queue: queueName, exchange: exchange, routingKey: EventTypes.SelectionParticipantSelectedV1, cancellationToken: stoppingToken);

                await channel.BasicQosAsync(0, 10, false, stoppingToken);

                logger.LogInformation("SelectionEventConsumer listening on {queue}", queueName);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delivery = await channel.BasicGetAsync(queueName, autoAck: false, cancellationToken: stoppingToken);
                    if (delivery is null)
                    {
                        await Task.Delay(400, stoppingToken);
                        continue;
                    }

                    try
                    {
                        var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
                        var env  = JsonSerializer.Deserialize<EventEnvelope<SelectionParticipantSelectedV1>>(json, JsonOpts);

                        if (env?.Data is null)
                        {
                            logger.LogWarning("Invalid envelope. Tag={tag}", delivery.DeliveryTag);
                            await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                            continue;
                        }

                        using var scope = sp.CreateScope();
                        var db  = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
                        var bus = scope.ServiceProvider.GetRequiredService<IEventPublisher>();
                        var d   = env.Data;

                        // UPSERT projeção
                        var sel = await db.SelectedRegistrations
                            .SingleOrDefaultAsync(x => x.RegistrationId == d.RegistrationId, stoppingToken);

                        if (sel is null)
                        {
                            sel = new SelectedRegistration(
                                registrationId: d.RegistrationId,
                                retreatId: d.RetreatId,
                                name: d.Name,
                                email: d.Email,
                                phone: d.Phone,
                                amount: d.Amount,
                                currency: d.Currency
                            );
                            await db.SelectedRegistrations.AddAsync(sel, stoppingToken);
                        }
                        else
                        {
                            sel.UpdateContact(d.Name, d.Email, d.Phone);
                            sel.UpdatePricing(d.Amount, d.Currency);
                            sel.Touch();
                        }

                        await db.SaveChangesAsync(stoppingToken);

                        // Dispara pedido de pagamento (Payment garante idempotência)
                        var req = new PaymentRequestedV1(
                            d.RegistrationId, d.RetreatId, d.Amount, d.Currency, d.Name, d.Email, d.Phone
                        );

                        await bus.PublishAsync(
                            type: EventTypes.PaymentRequestedV1,
                            source: "sam.notification",
                            data: req
                        );

                        await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing selected.v1. Tag={tag}", delivery.DeliveryTag);
                        await channel.BasicNackAsync(delivery.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
                    }
                }
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                logger.LogWarning(ex, "RabbitMQ indisponível. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException) { /* shutting down */ }
            catch (Exception ex)
            {
                logger.LogError(ex, "SelectionEventConsumer loop error. Retry 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("SelectionEventConsumer stopped.");
    }
}
