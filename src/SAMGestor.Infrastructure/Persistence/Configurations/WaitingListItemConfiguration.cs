using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class WaitingListItemConfiguration : IEntityTypeConfiguration<WaitingListItem>
{
    public void Configure(EntityTypeBuilder<WaitingListItem> builder)
    {
        builder.ToTable("waiting_list_items");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.RegistrationId)
            .HasColumnName("registration_id")
            .IsRequired();

        builder.Property(w => w.RetreatId)
            .HasColumnName("retreat_id")
            .IsRequired();

        builder.Property(w => w.Position)
            .HasColumnName("position")
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Um inscrito não aparece mais de uma vez na fila
        builder.HasIndex(w => w.RegistrationId).IsUnique();

        // A posição é única dentro de cada retiro
        builder.HasIndex(w => new { w.RetreatId, w.Position }).IsUnique();
    }
}