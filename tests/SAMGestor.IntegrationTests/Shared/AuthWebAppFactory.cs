using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SAMGestor.Infrastructure.Messaging.Outbox;

namespace SAMGestor.IntegrationTests.Shared;

public sealed class AuthWebAppFactory : PostgresWebAppFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        
        Environment.SetEnvironmentVariable("IT_TEST_AUTH", "false");
        Environment.SetEnvironmentVariable("IT_BYPASS_AUTHZ", "false");
        
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;


        base.ConfigureWebHost(builder);

        builder.UseEnvironment("Test");

        builder.ConfigureLogging(lb =>
        {
            lb.ClearProviders();
            lb.AddConsole();
            lb.SetMinimumLevel(LogLevel.Information);
        });

        
        builder.ConfigureServices(services =>
        {
            var hosted = services
                .Where(d => d.ServiceType == typeof(IHostedService) && d.ImplementationType == typeof(OutboxDispatcher))
                .ToList();

            foreach (var d in hosted)
                services.Remove(d);
            
            services.PostConfigureAll<JwtBearerOptions>(o =>
            {
                o.MapInboundClaims = false;
            });
        });
    }
}