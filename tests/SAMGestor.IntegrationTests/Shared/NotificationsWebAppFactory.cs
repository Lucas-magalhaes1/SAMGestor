using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SAMGestor.Application.Interfaces;
using SAMGestor.IntegrationTests.TestDoubles;

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
            services.RemoveAll<IEventBus>();

            services.AddSingleton<CapturingEventBus>();
            services.AddSingleton<IEventBus>(sp => sp.GetRequiredService<CapturingEventBus>());
        });
    }
}