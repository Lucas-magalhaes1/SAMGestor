using System.Text.Json;
using SAMGestor.Contracts;
using SAMGestor.Payment.Application.Abstractions;
using SAMGestor.Payment.Infrastructure.Persistence;

namespace SAMGestor.Payment.Infrastructure.Messaging.Outbox;

public sealed class OutboxEventBus : IEventBus
{
    private readonly PaymentDbContext _db;
    private static readonly JsonSerializerOptions JsonOpt = new(JsonSerializerDefaults.Web);

    public OutboxEventBus(PaymentDbContext db) => _db = db;

    public async Task EnqueueAsync<T>(string type, string source, T data, string? traceId = null, CancellationToken ct = default)
    {
        var envelope = EventEnvelope<T>.Create(type, source, data, traceId);
        var json = JsonSerializer.Serialize(envelope, JsonOpt);

        var msg = new OutboxMessage
        {
            Type = type,
            Source = source,
            TraceId = envelope.TraceId,
            Data = json
        };

        await _db.Set<OutboxMessage>().AddAsync(msg, ct);
    }
}