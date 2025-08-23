using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SAMGestor.Application.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Messaging.Outbox;

public sealed class OutboxEventBus(SAMContext db) : IEventBus
{
    public async Task EnqueueAsync<T>(string type, string source, T data, string? traceId = null, CancellationToken ct = default)
    {
        var envelope = SAMGestor.Contracts.EventEnvelope<T>.Create(type, source, data, traceId);
        var json = JsonSerializer.Serialize(envelope);

        var msg = new OutboxMessage
        {
            Type = type,
            Source = source,
            TraceId = envelope.TraceId,
            Data = json
        };

        await db.OutboxMessages.AddAsync(msg, ct);
        
        await db.SaveChangesAsync(ct);
        
        await db.Database.ExecuteSqlRawAsync("NOTIFY outbox_new;", ct);
    }
}