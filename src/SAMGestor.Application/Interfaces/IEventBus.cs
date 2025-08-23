namespace SAMGestor.Application.Interfaces;

public interface IEventBus
{
    Task EnqueueAsync<T>(string type, string source, T data, string? traceId = null, CancellationToken ct = default);
}