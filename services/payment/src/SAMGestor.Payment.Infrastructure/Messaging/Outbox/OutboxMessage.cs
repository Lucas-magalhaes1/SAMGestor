namespace SAMGestor.Payment.Infrastructure.Messaging.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Type { get; set; } = default!;          // routing key
    public string Source { get; set; } = "sam.payment";
    public string TraceId { get; set; } = Guid.NewGuid().ToString();
    public string Data { get; set; } = default!;          // JSON do EventEnvelope<T>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; } = 0;
    public string? LastError { get; set; }
}