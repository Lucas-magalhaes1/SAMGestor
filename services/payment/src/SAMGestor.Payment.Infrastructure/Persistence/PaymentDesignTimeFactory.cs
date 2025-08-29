using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace SAMGestor.Payment.Infrastructure.Persistence;

public class PaymentDesignTimeFactory : IDesignTimeDbContextFactory<PaymentDbContext>
{
    public PaymentDbContext CreateDbContext(string[] args)
    {
        var cs = Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                 ?? Environment.GetEnvironmentVariable("DEV_LOCAL_CS")
                 ?? "Host=localhost;Port=5432;Database=samgestor_db;Username=sam_user;Password=SuP3rS3nh4!";

        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseNpgsql(cs, npg =>
                npg.MigrationsHistoryTable("__EFMigrationsHistory", PaymentDbContext.Schema))
            .Options;

        return new PaymentDbContext(options);
    }
}