using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Infrastructure.Messaging.Outbox;

namespace SAMGestor.Infrastructure.Persistence;

public class SAMContext(DbContextOptions<SAMContext> options) : DbContext(options)
{
    public static readonly string Schema =
        Environment.GetEnvironmentVariable("DB_SCHEMA") ?? "core";


    public DbSet<User>               Users               => Set<User>();
    public DbSet<Family>             Families            => Set<Family>();
    public DbSet<Retreat>            Retreats            => Set<Retreat>();
    public DbSet<Registration>       Registrations       => Set<Registration>();
    public DbSet<Team>               Teams               => Set<Team>();
    public DbSet<TeamMember>         TeamMembers         => Set<TeamMember>();
    public DbSet<Payment>            Payments            => Set<Payment>();
    public DbSet<Tent>               Tents               => Set<Tent>();
    public DbSet<MessageSent>        MessagesSent        => Set<MessageSent>();
    public DbSet<MessageTemplate>    MessageTemplates    => Set<MessageTemplate>();
    public DbSet<ChangeLog>          ChangeLogs          => Set<ChangeLog>();
    public DbSet<RegionConfig>       RegionConfigs       => Set<RegionConfig>();
    public DbSet<BlockedCpf>         BlockedCpfs         => Set<BlockedCpf>();
    public DbSet<WaitingListItem>    WaitingListItems    => Set<WaitingListItem>();
    public DbSet<OutboxMessage>      OutboxMessages      => Set<OutboxMessage>();
    public DbSet<FamilyMember>       FamilyMembers       => Set<FamilyMember>();
    public DbSet<ServiceSpace>       ServiceSpaces       => Set<ServiceSpace>();
    public DbSet<ServiceRegistration>ServiceRegistrations=> Set<ServiceRegistration>();
    public DbSet<ServiceAssignment>  ServiceAssignments  => Set<ServiceAssignment>();
    public DbSet<ServiceRegistrationPayment>  ServiceRegistrationPayments  => Set<ServiceRegistrationPayment>();
    


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("core");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SAMContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}