using RabbitMQ.Client;

namespace SAMGestor.Payment.Infrastructure.Messaging.RabbitMq;

public sealed class RabbitMqConnection(RabbitMqOptions opt)
{
    private readonly ConnectionFactory _factory = new()
    {
        HostName = opt.HostName,
        UserName = opt.UserName,
        Password = opt.Password,
        AutomaticRecoveryEnabled = true
    };

    private IConnection? _connection;

    public async Task<IConnection> GetOrCreateAsync(CancellationToken ct = default)
    {
        if (_connection is { IsOpen: true }) return _connection;
        _connection = await _factory.CreateConnectionAsync("sam-payment-conn", ct);
        return _connection;
    }
}