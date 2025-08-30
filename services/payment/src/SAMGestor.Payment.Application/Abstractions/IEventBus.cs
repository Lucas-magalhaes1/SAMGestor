namespace SAMGestor.Payment.Application.Abstractions;

public interface IEventBus
{
    Task EnqueueAsync<T>(
        string type,        // routing key, : "payment.link.created.v1"
        string source,      // "sam.payment"
        T data,
        string? traceId = null,
        CancellationToken ct = default
    );
}