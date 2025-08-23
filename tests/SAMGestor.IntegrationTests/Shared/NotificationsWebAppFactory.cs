using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SAMGestor.Application.Interfaces;
using SAMGestor.Infrastructure.Messaging.Outbox;

namespace SAMGestor.IntegrationTests.Shared;

/// <summary>
/// Factory para testes de Notifications que capturam eventos em memória (sem Outbox).
/// </summary>
public class NotificationsWebAppFactory : PostgresWebAppFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            var busDescriptor = services.Single(d => d.ServiceType == typeof(IEventBus));
            services.Remove(busDescriptor);

            var capturing = new SAMGestor.IntegrationTests.TestDoubles.CapturingEventBus();
            services.AddSingleton(capturing);
            services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<SAMGestor.IntegrationTests.TestDoubles.CapturingEventBus>());
        });
    }
}