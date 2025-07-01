using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;


namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class TeamMemberConfiguration : IEntityTypeConfiguration<TeamMember>
{
    public void Configure(EntityTypeBuilder<TeamMember> builder)
    {
        builder.ToTable("team_members");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.RegistrationId).HasColumnName("registration_id").IsRequired();
        builder.Property(m => m.TeamId).HasColumnName("team_id").IsRequired();
        builder.Property(m => m.Role).HasColumnName("role").HasConversion<string>().IsRequired();

        builder.HasIndex(m => new { m.TeamId, m.RegistrationId }).IsUnique();
    }
}