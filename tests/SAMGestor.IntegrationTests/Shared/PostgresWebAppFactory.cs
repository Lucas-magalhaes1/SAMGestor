using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using SAMGestor.Infrastructure.Persistence;
using Testcontainers.PostgreSql;
using Xunit;

namespace SAMGestor.IntegrationTests.Shared;

public class PostgresWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private PostgreSqlContainer? _pg;
    private string? _dbName;
    private string? _connectionString;

    private readonly SemaphoreSlim _initGate = new(1, 1);
    private bool _initialized;

    public async Task InitializeAsync()
    {
        await _initGate.WaitAsync();
        try
        {
            if (_initialized) return;

            _pg = await PostgresContainerManager.AcquireAsync();

            _dbName = $"sam_it_{Guid.NewGuid():N}";

            await CreateDatabaseAsync(_pg, _dbName);

            var csb = new NpgsqlConnectionStringBuilder(_pg.GetConnectionString())
            {
                Database = _dbName
            };

            _connectionString = csb.ToString();

            _initialized = true;
        }
        finally
        {
            _initGate.Release();
        }
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            var dict = new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = _connectionString
            };

            cfg.AddInMemoryCollection(dict!);
        });

        builder.ConfigureServices(services =>
        {
            builder.ConfigureLogging(lb =>
            {
                lb.ClearProviders();
                lb.AddConsole();
                lb.SetMinimumLevel(LogLevel.Debug);

                lb.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
                lb.AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.Debug);
                lb.AddFilter("Npgsql", LogLevel.Debug);
                lb.AddFilter("SAMGestor", LogLevel.Debug);
            });

            services.RemoveAll<DbContextOptions<SAMContext>>();

            services.AddDbContext<SAMContext>(options =>
            {
                options.EnableDetailedErrors();
                options.EnableSensitiveDataLogging();

                options.UseNpgsql(_connectionString!, npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                });
            });

            if (IsTestAuthEnabled())
            {
                services.AddAuthentication(o =>
                    {
                        o.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
                        o.DefaultChallengeScheme = TestAuthHandler.SchemeName;
                    })
                    .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, TestAuthHandler>(
                        TestAuthHandler.SchemeName, _ => { });

                services.AddAuthorization(o =>
                {
                    o.FallbackPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
                        .AddAuthenticationSchemes(TestAuthHandler.SchemeName)
                        .RequireAuthenticatedUser()
                        .Build();
                });

                var bypass = (Environment.GetEnvironmentVariable("IT_BYPASS_AUTHZ") ?? "true")
                    .Trim().ToLowerInvariant() is "true" or "1" or "yes";

                if (bypass)
                {
                    services.RemoveAll<IAuthorizationService>();
                    services.AddSingleton<IAuthorizationService, AllowAllAuthorizationService>();
                }
            }
        });
    }
    
    protected override IHost CreateHost(IHostBuilder builder)
    {
        EnsureJwtConfigForTests();

        EnsureInitializedSync();

        var host = builder.Build();

        InitializeDatabaseAsync(host.Services).GetAwaiter().GetResult();

        host.Start();

        return host;
    }

    private void EnsureInitializedSync()
    {
        if (_initialized) return;
        InitializeAsync().GetAwaiter().GetResult();
    }

    private async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        if (_pg is null || _dbName is null || _connectionString is null)
            throw new InvalidOperationException("Factory não inicializada.");

        var initMode = (Environment.GetEnvironmentVariable("IT_DB_INIT") ?? "ensurecreated")
            .Trim().ToLowerInvariant();

        using var scope = services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<SAMContext>();

        if (initMode == "migrate")
        {
            await MigrateWithDiagnosticsAsync(ctx);
            return;
        }

        await ctx.Database.EnsureCreatedAsync();
    }

    private static async Task MigrateWithDiagnosticsAsync(SAMContext ctx)
    {
        var pending = (await ctx.Database.GetPendingMigrationsAsync()).ToList();
        if (pending.Count == 0)
        {
            await ctx.Database.MigrateAsync();
            return;
        }

        var migrator = ctx.GetService<IMigrator>();

        foreach (var migration in pending)
        {
            try
            {
                await migrator.MigrateAsync(migration);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Falhou na migration: {migration}");
                Console.WriteLine(ex);
                throw;
            }
        }
    }

    private static void EnsureJwtConfigForTests()
    {
        var legacySecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        var legacyIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
        var legacyAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
        var legacyAccessMin = Environment.GetEnvironmentVariable("JWT_ACCESS_MINUTES");
        var legacyRefreshDays = Environment.GetEnvironmentVariable("JWT_REFRESH_DAYS");

        SetIfEmpty("Jwt__Secret", legacySecret ?? "THIS_IS_A_TEST_SECRET_CHANGE_ME_1234567890_123456");
        SetIfEmpty("Jwt__Issuer", legacyIssuer ?? "SAMGestor.IT");
        SetIfEmpty("Jwt__Audience", legacyAudience ?? "SAMGestor.IT.Web");

        SetIfEmpty("Jwt__AccessTokenMinutes", legacyAccessMin ?? "60");
        SetIfEmpty("Jwt__ExpirationMinutes", legacyAccessMin ?? "60");
        SetIfEmpty("Jwt__RefreshDays", legacyRefreshDays ?? "30");
    }

    private static void SetIfEmpty(string key, string value)
    {
        var current = Environment.GetEnvironmentVariable(key);
        if (string.IsNullOrWhiteSpace(current))
            Environment.SetEnvironmentVariable(key, value);
    }

    private static bool IsTestAuthEnabled()
    {
        var v = (Environment.GetEnvironmentVariable("IT_TEST_AUTH") ?? "true")
            .Trim().ToLowerInvariant();
        return v is "true" or "1" or "yes";
    }

    // ===== Dispose =====
    Task IAsyncLifetime.DisposeAsync() => DisposeLifetimeAsync();

    public override async ValueTask DisposeAsync()
    {
        await DisposeLifetimeAsync();
        await base.DisposeAsync();
    }

    private async Task DisposeLifetimeAsync()
    {
        await DisposeCoreAsync();
        await PostgresContainerManager.ReleaseAsync();
    }

    private async Task DisposeCoreAsync()
    {
        try
        {
            if (_pg is null || _dbName is null) return;

            NpgsqlConnection.ClearAllPools();

            await DropDatabaseAsync(_pg, _dbName);
        }
        catch
        {
        }
    }

    // ===== DB admin helpers =====
    private static async Task CreateDatabaseAsync(PostgreSqlContainer pg, string dbName)
    {
        var admin = new NpgsqlConnectionStringBuilder(pg.GetConnectionString())
        {
            Database = "postgres"
        }.ToString();

        await using var conn = new NpgsqlConnection(admin);
        await conn.OpenAsync();

        try
        {
            await using var cmd = new NpgsqlCommand($@"CREATE DATABASE ""{dbName}"";", conn);
            await cmd.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == "42P04") // duplicate_database
        {
            // ok
        }
    }

    private static async Task DropDatabaseAsync(PostgreSqlContainer pg, string dbName)
    {
        var admin = new NpgsqlConnectionStringBuilder(pg.GetConnectionString())
        {
            Database = "postgres"
        }.ToString();

        await using var conn = new NpgsqlConnection(admin);
        await conn.OpenAsync();

        // Postgres 13+ suporta FORCE (Postgres 16 ok)
        try
        {
            await using var cmd = new NpgsqlCommand($@"DROP DATABASE IF EXISTS ""{dbName}"" WITH (FORCE);", conn);
            await cmd.ExecuteNonQueryAsync();
            return;
        }
        catch
        {
            // fallback: mata conexões e drop normal
            await using (var kill = new NpgsqlCommand($@"
                SELECT pg_terminate_backend(pid)
                FROM pg_stat_activity
                WHERE datname = '{dbName}' AND pid <> pg_backend_pid();", conn))
            {
                await kill.ExecuteNonQueryAsync();
            }

            await using (var drop = new NpgsqlCommand($@"DROP DATABASE IF EXISTS ""{dbName}"";", conn))
            {
                await drop.ExecuteNonQueryAsync();
            }
        }
    }

    // ===== container compartilhado (1 por suíte) =====
    private static class PostgresContainerManager
    {
        private static readonly SemaphoreSlim Gate = new(1, 1);
        private static PostgreSqlContainer? _container;
        private static int _refCount;

        public static async Task<PostgreSqlContainer> AcquireAsync()
        {
            await Gate.WaitAsync();
            try
            {
                if (_container is null)
                {
                    var image = Environment.GetEnvironmentVariable("IT_PG_IMAGE")
                                ?? Environment.GetEnvironmentVariable("PG_TEST_IMAGE")
                                ?? "postgres:16";

                    _container = new PostgreSqlBuilder()
                        .WithImage(image)
                        .WithDatabase("sam_tests")
                        .WithUsername("postgres")
                        .WithPassword("postgres")
                        .Build();

                    await _container.StartAsync();
                }

                _refCount++;
                return _container;
            }
            finally
            {
                Gate.Release();
            }
        }

        public static async Task ReleaseAsync()
        {
            await Gate.WaitAsync();
            try
            {
                _refCount = Math.Max(0, _refCount - 1);

            }
            finally
            {
                Gate.Release();
            }
        }
    }
}
