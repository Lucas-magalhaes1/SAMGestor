using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;


namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.ToTable("teams");
        builder.HasKey(t => t.Id);

        builder.OwnsOne(t => t.Name, n =>
        {
            n.Property(p => p.Value).HasColumnName("name").HasMaxLength(120).IsRequired();
        });

        builder.Property(t => t.MemberLimit).HasColumnName("member_limit").IsRequired();
        builder.Property(t => t.RetreatId).HasColumnName("retreat_id").IsRequired();
        
        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.MinMembers)
            .HasColumnName("min_members")
            .IsRequired();

        builder.HasMany(t => t.Members).WithOne().HasForeignKey(m => m.TeamId);
    }
}