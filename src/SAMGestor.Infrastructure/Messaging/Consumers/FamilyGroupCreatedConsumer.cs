using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using SAMGestor.Contracts;
using SAMGestor.Infrastructure.Messaging.RabbitMq;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Messaging.Consumers;

public sealed class FamilyGroupCreatedConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<FamilyGroupCreatedConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "core.familygroups";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FamilyGroupCreatedConsumer starting…");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.ExchangeDeclareAsync(opt.Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: stoppingToken);
                await channel.QueueBindAsync(QueueName, opt.Exchange, EventTypes.FamilyGroupCreatedV1, cancellationToken: stoppingToken);
                await channel.BasicQosAsync(0, 10, false, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delivery = await channel.BasicGetAsync(QueueName, autoAck: false, cancellationToken: stoppingToken);
                    if (delivery is null) { await Task.Delay(400, stoppingToken); continue; }

                    try
                    {
                        var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
                        var env  = JsonSerializer.Deserialize<EventEnvelope<FamilyGroupCreatedV1>>(json, JsonOpts);

                        if (env?.Data is null)
                        {
                            await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                            continue;
                        }

                        using var scope = sp.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<SAMContext>();

                        var fam = await db.Families.SingleOrDefaultAsync(f => f.Id == env.Data.FamilyId && f.RetreatId == env.Data.RetreatId, stoppingToken);
                        if (fam is not null)
                        {
                            fam.MarkGroupActive(
                                link: env.Data.Link,
                                externalId: env.Data.ExternalId,
                                channel: env.Data.Channel,
                                createdAt: env.Data.CreatedAt,
                                notifiedAt: env.Data.CreatedAt // como o stub já notifica por e-mail agora
                            );

                            await db.SaveChangesAsync(stoppingToken);
                        }

                        await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing FamilyGroupCreatedV1");
                        await channel.BasicNackAsync(delivery.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "FamilyGroupCreatedConsumer loop error. Retry soon…");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
