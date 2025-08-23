using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SAMGestor.Application.Interfaces;
using SAMGestor.Infrastructure.Messaging.Outbox;

namespace SAMGestor.IntegrationTests.Shared;

/// <summary>
/// Factory específica para testes que validam gravação no Outbox real.
/// Garante que o IEventBus usado nos requests é o OutboxEventBus.
/// </summary>
public class OutboxWebAppFactory : PostgresWebAppFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            var busDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IEventBus));
            if (busDescriptor is not null) services.Remove(busDescriptor);
            
            services.AddScoped<IEventBus, OutboxEventBus>();
        });
        
        builder.UseEnvironment("Development");
        builder.ConfigureLogging(lb =>
        {
            lb.ClearProviders();
            lb.AddConsole();
            lb.SetMinimumLevel(LogLevel.Information);
        });
    }
}