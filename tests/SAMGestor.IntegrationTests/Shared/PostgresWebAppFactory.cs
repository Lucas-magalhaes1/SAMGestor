
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;                              
using SAMGestor.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

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
                options.UseNpgsql(connectionString: cs, npgsql => { /* ex: npgsql.EnableRetryOnFailure(); */ });
            });
            
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<SAMContext>();

            ctx.Database.OpenConnection();
            ctx.Database.ExecuteSqlRaw($"""CREATE SCHEMA IF NOT EXISTS "{_schema}";""");
            ctx.Database.Migrate();
        });
    }

    public async Task DisposeAsync()
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
        catch { /* não falhar teardown do teste */ }

        if (_pg is not null)
            await _pg.DisposeAsync();
    }
}
