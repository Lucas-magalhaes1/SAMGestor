using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using SAMGestor.Contracts;
using SAMGestor.Infrastructure.Messaging.RabbitMq;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Messaging.Consumers;

public sealed class FamilyGroupCreateFailedConsumer(
    RabbitMqOptions opt,
    RabbitMqConnection conn,
    ILogger<FamilyGroupCreateFailedConsumer> logger,
    IServiceProvider sp
) : BackgroundService
{
    private const string QueueName = "core.familygroups.failed";
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FamilyGroupCreateFailedConsumer starting…");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var connection = await conn.GetOrCreateAsync(stoppingToken);
                await using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.ExchangeDeclareAsync(opt.Exchange, ExchangeType.Topic, true, cancellationToken: stoppingToken);
                await channel.QueueDeclareAsync(
                    QueueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    noWait: false,
                    cancellationToken: stoppingToken
                );
                await channel.QueueBindAsync(QueueName, opt.Exchange, EventTypes.FamilyGroupCreateFailedV1, cancellationToken: stoppingToken);
                await channel.BasicQosAsync(0, 10, false, stoppingToken);

                while (!stoppingToken.IsCancellationRequested)
                {
                    var delivery = await channel.BasicGetAsync(QueueName, autoAck: false, cancellationToken: stoppingToken);
                    if (delivery is null) { await Task.Delay(400, stoppingToken); continue; }

                    try
                    {
                        var json = Encoding.UTF8.GetString(delivery.Body.ToArray());
                        var env  = JsonSerializer.Deserialize<EventEnvelope<FamilyGroupCreateFailedV1>>(json, JsonOpts);
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
                            fam.MarkGroupFailed();
                            await db.SaveChangesAsync(stoppingToken);
                        }

                        await channel.BasicAckAsync(delivery.DeliveryTag, false, stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error processing FamilyGroupCreateFailedV1");
                        await channel.BasicNackAsync(delivery.DeliveryTag, false, requeue: false, cancellationToken: stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "FamilyGroupCreateFailedConsumer loop error. Retry soon…");
                await Task.Delay(2000, stoppingToken);
            }
        }
    }
}
