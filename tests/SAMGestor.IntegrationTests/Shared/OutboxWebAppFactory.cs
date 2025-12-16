using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Infrastructure.Messaging.Outbox;

namespace SAMGestor.IntegrationTests.Shared;

/// <summary>
/// Factory específica para testes que validam gravação no Outbox real.
/// </summary>
public class OutboxWebAppFactory : PostgresWebAppFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEventBus>();
            services.AddScoped<IEventBus, OutboxEventBus>();
        });

        builder.ConfigureLogging(lb =>
        {
            lb.ClearProviders();
            lb.AddConsole();
            lb.SetMinimumLevel(LogLevel.Information);
        });
    }
}