using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Infrastructure.Messaging.Outbox;
using SAMGestor.Infrastructure.Messaging.RabbitMq;

namespace SAMGestor.IntegrationTests.Shared;

public class RabbitOutboxWebAppFactory : PostgresWebAppFactory, IAsyncDisposable
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

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            var busDesc = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
            if (busDesc is not null) services.Remove(busDesc);
            services.AddScoped<IEventBus, OutboxEventBus>();

            if (!services.Any(d => d.ServiceType == typeof(OutboxDispatcher)))
                services.AddHostedService<OutboxDispatcher>();
            
            foreach (var d in services.Where(d =>
                     d.ServiceType == typeof(RabbitMqOptions) ||
                     d.ServiceType.FullName == "Microsoft.Extensions.Options.IOptions`1"
                     && d.ImplementationType?.GenericTypeArguments.FirstOrDefault() == typeof(RabbitMqOptions)).ToList())
            {
                services.Remove(d);
            }
            
            services.AddSingleton(new RabbitMqOptions
            {
                HostName = RabbitHost,
                Port     = RabbitPort,
                UserName = "guest",
                Password = "guest",
                Exchange = "sam.topic"
            });
        });

        builder.UseEnvironment("Development");
        builder.ConfigureLogging(lb =>
        {
            lb.ClearProviders();
            lb.AddConsole();
            lb.SetMinimumLevel(LogLevel.Information);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _rabbit.DisposeAsync();
        await base.DisposeAsync();
    }
}
