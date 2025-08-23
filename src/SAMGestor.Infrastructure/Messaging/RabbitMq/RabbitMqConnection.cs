using RabbitMQ.Client;

namespace SAMGestor.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqConnection(RabbitMqOptions opt)
{
    private readonly ConnectionFactory _factory = new()
    {
        HostName = opt.HostName,
        Port     = opt.Port,
        UserName = opt.UserName,
        Password = opt.Password,
    };

    private IConnection? _connection;

    public async Task<IConnection> GetOrCreateAsync(CancellationToken ct = default)
    {
        if (_connection is { IsOpen: true })
            return _connection;

        _connection = await _factory.CreateConnectionAsync("sam-connection", ct);
        return _connection;
    }
}