using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class TentAssignmentConfiguration : IEntityTypeConfiguration<TentAssignment>
{
    public void Configure(EntityTypeBuilder<TentAssignment> b)
    {
        b.ToTable("tent_assignments");
        b.HasKey(x => x.Id);

        b.Property(x => x.TentId)
            .HasColumnName("tent_id")
            .IsRequired();

        b.Property(x => x.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired();

        b.Property(x => x.Position)
            .HasColumnName("position"); 

        b.Property(x => x.AssignedAt)
            .HasColumnName("assigned_at")
            .IsRequired();

        b.Property(x => x.AssignedBy)
            .HasColumnName("assigned_by");

        // Índices
        b.HasIndex(x => x.TentId);
        b.HasIndex(x => x.RegistrationId)
            .IsUnique()
            .HasDatabaseName("ux_tent_assignments_registration");

        // FKs (sem navegações explícitas no domínio)
        b.HasOne<Tent>()
            .WithMany()
            .HasForeignKey(x => x.TentId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne<Registration>()
            .WithMany()
            .HasForeignKey(x => x.RegistrationId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}