using System.Text;
using RabbitMQ.Client;

namespace SAMGestor.Infrastructure.Messaging.RabbitMq;

public sealed class EventPublisher(RabbitMqOptions opt, RabbitMqConnection conn)
{
    public async Task PublishAsync(string routingKey, string json, CancellationToken ct = default)
    {
        var connection = await conn.GetOrCreateAsync(ct);
        
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);

        await channel.ExchangeDeclareAsync(
            opt.Exchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: ct
        );

        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        var body = Encoding.UTF8.GetBytes(json);

        await channel.BasicPublishAsync(
            exchange: opt.Exchange,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct
        );
    }
}