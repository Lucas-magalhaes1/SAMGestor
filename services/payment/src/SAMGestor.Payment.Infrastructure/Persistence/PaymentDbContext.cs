using Microsoft.EntityFrameworkCore;
using SAMGestor.Payment.Infrastructure.Messaging.Outbox;
using PaymentEntity = SAMGestor.Payment.Domain.Entities.Payment;


namespace SAMGestor.Payment.Infrastructure.Persistence;

public sealed class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public static readonly string Schema =
        Environment.GetEnvironmentVariable("DB_SCHEMA") ?? "payment";

    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PaymentDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}