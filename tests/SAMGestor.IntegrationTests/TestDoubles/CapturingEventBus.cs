using SAMGestor.Application.Interfaces;

namespace SAMGestor.IntegrationTests.TestDoubles;

public sealed class CapturingEventBus : IEventBus
{
    public sealed record Enqueued(string Type, string Source, object Data, string? TraceId);

    private readonly List<Enqueued> _items = new();
    private readonly object _gate = new();

    public IReadOnlyList<Enqueued> Items
    {
        get { lock (_gate) return _items.ToArray(); }
    }

    public void Clear()
    {
        lock (_gate) _items.Clear();
    }

    private Task EnqueueInternal(string type, string source, object data, string? traceId, CancellationToken ct)
    {
        lock (_gate) _items.Add(new Enqueued(type, source, data, traceId));
        return Task.CompletedTask;
    }

    public Task EnqueueAsync(string type, string source, object data, CancellationToken ct = default)
        => EnqueueInternal(type, source, data, traceId: null, ct);

    public Task EnqueueAsync<T>(string type, string source, T data, string? traceId = null, CancellationToken ct = default)
        => EnqueueInternal(type, source, data!, traceId, ct);
}