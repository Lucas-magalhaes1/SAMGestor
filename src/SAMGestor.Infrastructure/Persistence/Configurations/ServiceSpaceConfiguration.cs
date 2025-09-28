using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class ServiceSpaceConfiguration : IEntityTypeConfiguration<ServiceSpace>
{
    public void Configure(EntityTypeBuilder<ServiceSpace> builder)
    {
        builder.ToTable("service_spaces");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.RetreatId).IsRequired();

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(x => x.Description)
            .HasMaxLength(512);

        builder.Property(x => x.MinPeople).IsRequired();
        builder.Property(x => x.MaxPeople).IsRequired();

        builder.Property(x => x.IsLocked).HasDefaultValue(false).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true).IsRequired();
        
        builder.HasIndex(x => new { x.RetreatId, x.Name }).IsUnique();

        builder.HasIndex(x => new { x.RetreatId, x.IsActive });
    }
}