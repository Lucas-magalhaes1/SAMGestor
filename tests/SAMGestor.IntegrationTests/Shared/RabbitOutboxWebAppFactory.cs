using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Infrastructure.Messaging.Outbox;
using SAMGestor.Infrastructure.Messaging.RabbitMq;
using Xunit;

namespace SAMGestor.IntegrationTests.Shared;

public class RabbitOutboxWebAppFactory : PostgresWebAppFactory, IAsyncLifetime
{
    private readonly RabbitContainer _rabbit = new();

    public string RabbitHost { get; private set; } = "localhost";
    public int RabbitPort { get; private set; }

    public RabbitOutboxWebAppFactory()
    {
        _rabbit.StartAsync().GetAwaiter().GetResult();
        RabbitHost = _rabbit.Host;
        RabbitPort = _rabbit.Port;
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await base.InitializeAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _rabbit.DisposeAsync();
        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Outbox:PublishTimeoutSeconds"] = "5",
                ["Outbox:PollIntervalSeconds"] = "1",
                ["Outbox:UseListenNotify"] = "false",

                ["MessageBus:Host"] = RabbitHost,
                ["MessageBus:HostName"] = RabbitHost,
                ["MessageBus:Port"] = RabbitPort.ToString(),
                ["MessageBus:User"] = "guest",
                ["MessageBus:UserName"] = "guest",
                ["MessageBus:Pass"] = "guest",
                ["MessageBus:Password"] = "guest",
                ["MessageBus:Exchange"] = "sam.topic",

                ["RabbitMq:HostName"] = RabbitHost,
                ["RabbitMq:Port"] = RabbitPort.ToString(),
                ["RabbitMq:UserName"] = "guest",
                ["RabbitMq:Password"] = "guest",
                ["RabbitMq:Exchange"] = "sam.topic",
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEventBus>();
            services.AddScoped<IEventBus, OutboxEventBus>();

            var hasDispatcherHosted = services.Any(d =>
                d.ServiceType == typeof(IHostedService) &&
                d.ImplementationType == typeof(OutboxDispatcher));

            if (!hasDispatcherHosted)
                services.AddHostedService<OutboxDispatcher>();

            // remove opções/conexões/publicador pra evitar “vazar” config antiga
            services.RemoveAll<RabbitMqOptions>();
            services.RemoveAll<RabbitMqConnection>();
            services.RemoveAll<EventPublisher>();

            services.AddSingleton(new RabbitMqOptions
            {
                HostName = RabbitHost,
                Port = RabbitPort,
                UserName = "guest",
                Password = "guest",
                Exchange = "sam.topic",
                ServingPaymentQueue = "core.payment.serving"
            });

            services.AddOptions<RabbitMqOptions>();
            services.PostConfigure<RabbitMqOptions>(o =>
            {
                o.HostName = RabbitHost;
                o.Port = RabbitPort;
                o.UserName = "guest";
                o.Password = "guest";
                o.Exchange = "sam.topic";
                if (string.IsNullOrWhiteSpace(o.ServingPaymentQueue))
                    o.ServingPaymentQueue = "core.payment.serving";
            });

            services.AddSingleton<RabbitMqConnection>();
            services.AddSingleton<EventPublisher>();
        });

        builder.ConfigureLogging(lb =>
        {
            lb.ClearProviders();
            lb.AddConsole();
            lb.SetMinimumLevel(LogLevel.Information);
        });
    }
}
