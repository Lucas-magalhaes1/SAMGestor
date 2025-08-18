namespace SAMGestor.Notification.Application.Abstractions;

public interface IEventPublisher
{
    Task PublishAsync<T>(string type, string source, T data, string? traceId = null, CancellationToken ct = default);
}