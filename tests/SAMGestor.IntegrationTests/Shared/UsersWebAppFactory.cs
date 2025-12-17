using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace SAMGestor.IntegrationTests.Shared;

public sealed class UsersWebAppFactory : PostgresWebAppFactory
{
    private readonly MailhogContainer _mailhog = new();
    private readonly object _gate = new();
    private bool _started;

    public string MailhogHost { get; private set; } = "localhost";
    public int MailhogSmtpPort { get; private set; }
    public int MailhogHttpPort { get; private set; }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        EnsureMailhogStartedSync();
        return base.CreateHost(builder);
    }

    private void EnsureMailhogStartedSync()
    {
        lock (_gate)
        {
            if (_started) return;

            _mailhog.StartAsync().GetAwaiter().GetResult();
            MailhogHost = _mailhog.Host;
            MailhogSmtpPort = _mailhog.SmtpPort;
            MailhogHttpPort = _mailhog.HttpPort;

            _started = true;
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // ✅ caso seu app use SMTP (docker-compose usa Smtp__Host/Smtp__Port)
                ["Smtp:Host"] = MailhogHost,
                ["Smtp:Port"] = MailhogSmtpPort.ToString(),

                // ✅ caso seu app use HTTP sender (você tinha "Email:BaseUrl" no appsettings)
                ["Email:BaseUrl"] = $"http://{MailhogHost}:{MailhogHttpPort}/",
                ["Email:SendPath"] = "api/v2/messages"
            });
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await _mailhog.DisposeAsync();
        await base.DisposeAsync();
    }
}