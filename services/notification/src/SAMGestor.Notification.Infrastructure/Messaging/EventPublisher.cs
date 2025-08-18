using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using SAMGestor.Notification.Application.Abstractions;

namespace SAMGestor.Notification.Infrastructure.Messaging;

public class EventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly RabbitMqOptions _opt;
    private readonly RabbitMqConnection _conn;

    public EventPublisher(RabbitMqOptions opt, RabbitMqConnection conn)
    {
        _opt = opt;
        _conn = conn;
    }

    public async Task PublishAsync<T>(string type, string source, T data, string? traceId = null, CancellationToken ct = default)
    {
        var connection = await _conn.GetOrCreateAsync().ConfigureAwait(false);
        // v7: o CancellationToken é o segundo parâmetro; use nomeado
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct).ConfigureAwait(false);

        await channel.ExchangeDeclareAsync(
            _opt.Exchange,
            ExchangeType.Topic,
            durable: true,
            cancellationToken: ct
        ).ConfigureAwait(false);

        var envelope = SAMGestor.Contracts.EventEnvelope<T>.Create(type, source, data, traceId);
        var json = JsonSerializer.Serialize(envelope);
        var body = Encoding.UTF8.GetBytes(json);

        // v7: BasicProperties via 'new' e DeliveryModes enum
        var props = new BasicProperties
        {
            ContentType = "application/json",
            DeliveryMode = DeliveryModes.Persistent
        };

        // v7: assinatura inclui 'mandatory' antes de basicProperties
        await channel.BasicPublishAsync(
            exchange: _opt.Exchange,
            routingKey: type,
            mandatory: false,
            basicProperties: props,
            body: body,
            cancellationToken: ct
        ).ConfigureAwait(false);
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}