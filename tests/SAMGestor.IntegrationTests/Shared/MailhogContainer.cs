using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace SAMGestor.IntegrationTests.Shared;

public sealed class MailhogContainer : IAsyncDisposable
{
    public string Host { get; private set; } = "localhost";
    public int SmtpPort { get; private set; }     // 1025
    public int HttpPort { get; private set; }     // 8025

    private IContainer? _container;

    public async Task StartAsync()
    {
        var c = new ContainerBuilder()
            .WithImage("mailhog/mailhog:latest")
            .WithName($"samtests-mailhog-{Guid.NewGuid():N}")
            .WithPortBinding(0, 1025)   // SMTP
            .WithPortBinding(0, 8025)   // HTTP UI/API
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1025))
            .Build();

        await c.StartAsync();
        _container = c;

        Host = "localhost";
        SmtpPort = c.GetMappedPublicPort(1025);
        HttpPort = c.GetMappedPublicPort(8025);
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
            await _container.DisposeAsync();
    }
}