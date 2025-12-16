using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace SAMGestor.IntegrationTests.Shared;

public sealed class RabbitContainer : IAsyncDisposable
{
    public string Host { get; private set; } = default!;
    public int Port { get; private set; }

    private IContainer? _container;

    public async Task StartAsync()
    {
        var rabbit = new ContainerBuilder()
            .WithImage("rabbitmq:3.13-alpine")
            .WithName($"samtests-rabbit-{Guid.NewGuid():N}")
            .WithPortBinding(0, 5672)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
            .Build();

        await rabbit.StartAsync();

        _container = rabbit;
        Host = "localhost";
        Port = rabbit.GetMappedPublicPort(5672);
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}