using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SAMGestor.Notification.Infrastructure.Persistence;

public class NotificationDesignTimeDbContextFactory
    : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                 ?? Environment.GetEnvironmentVariable("DEV_LOCAL_CS")
                 ?? "Host=localhost;Port=5432;Database=samgestor_db;Username=sam_user;Password=SuP3rS3nh4!";

        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseNpgsql(cs, npg =>
                npg.MigrationsHistoryTable("__EFMigrationsHistory", NotificationDbContext.Schema))
            .Options;

        return new NotificationDbContext(options);
    }
}