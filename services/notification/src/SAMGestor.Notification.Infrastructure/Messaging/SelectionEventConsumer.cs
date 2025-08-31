using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAMGestor.Contracts;
using SAMGestor.Notification.Application.Orchestrators;

namespace SAMGestor.Notification.Infrastructure.Messaging;

public sealed class SelectionEventConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<SelectionEventConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SelectionEventConsumer starting…");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
                
                await channel.ExchangeDeclareAsync(opt.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(
                    queue: opt.SelectionQueue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: stoppingToken);
                
                await channel.QueueBindAsync(
                    queue: opt.SelectionQueue,
                    exchange: opt.Exchange,
                    routingKey: opt.SelectionRoutingKey,
                    cancellationToken: stoppingToken);
                
                await channel.QueueBindAsync(
                    queue: opt.SelectionQueue,
                    exchange: opt.Exchange,
                    routingKey: EventTypes.SelectionParticipantSelectedV1,
                    cancellationToken: stoppingToken);

                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

                logger.LogInformation("SelectionEventConsumer listening on queue {queue}", opt.SelectionQueue);
                
                while (!stoppingToken.IsCancellationRequested)
                {
                    var delivery = await channel.BasicGetAsync(opt.SelectionQueue, autoAck: false, cancellationToken: stoppingToken);

                    if (delivery is null)
                    {
                        await Task.Delay(400, stoppingToken);
                        continue;
                    }

                    try
                    {
                        var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
                        var env = JsonSerializer.Deserialize<EventEnvelope<SelectionParticipantSelectedV1>>(json, JsonOpts);

                        if (env?.Data is null)
                        {
                            logger.LogWarning("Invalid envelope or null data. DeliveryTag={tag}", delivery.DeliveryTag);
                            await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                            continue;
                        }

                        using var scope = sp.CreateScope();
                        var orchestrator = scope.ServiceProvider.GetRequiredService<NotificationOrchestrator>();
                        await orchestrator.OnParticipantSelectedAsync(env.Data, stoppingToken);

                        await channel.BasicAckAsync(delivery.DeliveryTag, multiple: false, cancellationToken: stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing selection event. DeliveryTag={tag}", delivery.DeliveryTag);
                        
                        await channel.BasicNackAsync(delivery.DeliveryTag, multiple: false, requeue: false, cancellationToken: stoppingToken);
                    }
                }
            }
            catch (RabbitMQ.Client.Exceptions.BrokerUnreachableException ex)
            {
                logger.LogWarning(ex, "RabbitMQ indisponível. Retentando em 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro no loop do SelectionEventConsumer. Retry em 5s…");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        logger.LogInformation("SelectionEventConsumer stopped.");
    }
}
