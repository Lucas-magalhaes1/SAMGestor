using RabbitMQ.Client;

namespace SAMGestor.Notification.Infrastructure.Messaging;

public sealed class RabbitMqConnection : IAsyncDisposable
{
    private readonly ConnectionFactory _factory;
    private IConnection? _connection;
    private readonly SemaphoreSlim _mutex = new(1, 1);

    public RabbitMqConnection(RabbitMqOptions opt)
    {
        _factory = new ConnectionFactory
        {
            HostName = opt.HostName,
            UserName = opt.UserName,
            Password = opt.Password,
            AutomaticRecoveryEnabled = true   
        };
    }
    
    public async Task<IConnection> GetOrCreateAsync(CancellationToken ct = default)
    {
        if (_connection is { IsOpen: true }) return _connection;

        await _mutex.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_connection is { IsOpen: true }) return _connection;
            
            _connection = await _factory.CreateConnectionAsync("sam-notification-conn", ct)
                .ConfigureAwait(false);
            return _connection;
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_connection != null)
        {
            try { await _connection.CloseAsync().ConfigureAwait(false); } catch { }
            await _connection.DisposeAsync().ConfigureAwait(false);
        }
        _mutex.Dispose();
    }
}