using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence;

public class SAMContext : DbContext
{
    public SAMContext(DbContextOptions<SAMContext> options) : base(options) { }

   
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SAMContext).Assembly);
    }
}