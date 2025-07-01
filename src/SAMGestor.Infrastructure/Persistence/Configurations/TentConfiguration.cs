using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class TentConfiguration : IEntityTypeConfiguration<Tent>
{
    public void Configure(EntityTypeBuilder<Tent> builder)
    {
        builder.ToTable("tents");
        builder.HasKey(t => t.Id);

        builder.OwnsOne(t => t.Number, n =>
        {
            n.Property(p => p.Value).HasColumnName("number").IsRequired();
        });

        builder.Property(t => t.Category).HasColumnName("category").HasConversion<string>().IsRequired();
        builder.Property(t => t.Capacity).HasColumnName("capacity").IsRequired();
        builder.Property(t => t.RetreatId).HasColumnName("retreat_id").IsRequired();
    }
}