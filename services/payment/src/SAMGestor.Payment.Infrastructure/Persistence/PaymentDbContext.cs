using Microsoft.EntityFrameworkCore;
using PaymentEntity = SAMGestor.Payment.Domain.Entities.Payment;


namespace SAMGestor.Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    // Alinha com o Notification: permite sobrescrever via env DB_SCHEMA
    public static readonly string Schema =
        Environment.GetEnvironmentVariable("DB_SCHEMA") ?? "payment";

    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}