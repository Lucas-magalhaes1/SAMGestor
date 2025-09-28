using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class ServiceAssignmentConfiguration : IEntityTypeConfiguration<ServiceAssignment>
{
    public void Configure(EntityTypeBuilder<ServiceAssignment> builder)
    {
        builder.ToTable("service_assignments");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceSpaceId)
               .HasColumnName("service_space_id")
               .IsRequired();

        builder.Property(x => x.ServiceRegistrationId)
               .HasColumnName("service_registration_id")
               .IsRequired();

        builder.Property(x => x.Role)
               .HasColumnName("role")        
               .HasConversion<int>()          
               .IsRequired();

        builder.Property(x => x.AssignedAt)
               .HasColumnName("assigned_at")
               .IsRequired();

        builder.HasOne<ServiceSpace>()
            .WithMany()
            .HasForeignKey(x => x.ServiceSpaceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<ServiceRegistration>()
            .WithMany()
            .HasForeignKey(x => x.ServiceRegistrationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.ServiceRegistrationId).IsUnique();
        builder.HasIndex(x => new { x.ServiceSpaceId, x.Role });
        builder.HasIndex(x => x.ServiceSpaceId);
        
        builder.HasIndex(x => x.ServiceSpaceId)
            .HasDatabaseName("UX_service_assignments_one_coordinator_per_space")
            .IsUnique()
            .HasFilter($"role = {(int)ServiceRole.Coordinator}");

        builder.HasIndex(x => x.ServiceSpaceId)
            .HasDatabaseName("UX_service_assignments_one_vice_per_space")
            .IsUnique()
            .HasFilter($"role = {(int)ServiceRole.Vice}");
    }
}
