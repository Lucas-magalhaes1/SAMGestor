using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace SAMGestor.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SAMContext>
{
    public SAMContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory()) // usa o diretório da API como base
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = configuration.GetConnectionString("Default")
                 ?? "Host=localhost;Port=5432;Database=samgestor_db;Username=sam_user;Password=SuP3rS3nh4!";

        var schema = Environment.GetEnvironmentVariable("DB_SCHEMA") ?? "core";

        var options = new DbContextOptionsBuilder<SAMContext>()
            .UseNpgsql(cs, o => o.MigrationsHistoryTable("__EFMigrationsHistory", schema))
            .Options;

        return new SAMContext(options);
    }
}