using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SAMGestor.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;
using System.Linq;

namespace SAMGestor.IntegrationTests.Shared;

public class PostgresWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _pg;
    private string? _schema;

    public async Task InitializeAsync()
    {
        _pg = new PostgreSqlBuilder()
            .WithImage(Environment.GetEnvironmentVariable("PG_TEST_IMAGE") ?? "postgres:16")
            .WithDatabase("sam_tests")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

        await _pg.StartAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<SAMContext>));
            services.Remove(descriptor);

            _schema = $"it_{Guid.NewGuid():N}";
            var baseCs = _pg!.GetConnectionString();
            var cs = $"{baseCs};Search Path={_schema}";

            services.AddDbContext<SAMContext>(options =>
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();
                options.UseNpgsql(connectionString: cs, npgsql => { });
            });

            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<SAMContext>();

            ctx.Database.OpenConnection();
#pragma warning disable EF1002
            ctx.Database.ExecuteSqlRaw($"""CREATE SCHEMA IF NOT EXISTS "{_schema}";""");
#pragma warning restore EF1002
            ctx.Database.Migrate();
        });
    }

    private async Task DisposeCoreAsync()
    {
        try
        {
            if (_schema is not null && _pg is not null)
            {
                using var conn = new NpgsqlConnection(_pg.GetConnectionString());
                await conn.OpenAsync();
                using var cmd = new NpgsqlCommand($"""DROP SCHEMA IF EXISTS "{_schema}" CASCADE;""", conn);
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch { }

        if (_pg is not null)
            await _pg.DisposeAsync();
    }

    async Task IAsyncLifetime.DisposeAsync() => await DisposeCoreAsync();

    public override async ValueTask DisposeAsync()
    {
        await DisposeCoreAsync();
        await base.DisposeAsync();
    }
}
