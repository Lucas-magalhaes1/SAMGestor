using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class FamilyConfiguration : IEntityTypeConfiguration<Family>
{
    public void Configure(EntityTypeBuilder<Family> builder)
    {
        builder.ToTable("families");
        builder.HasKey(f => f.Id);

        builder.OwnsOne(f => f.Name, n =>
        {
            n.Property(p => p.Value).HasColumnName("name").HasMaxLength(120).IsRequired();
        });

        builder.Property(f => f.GodfatherCount).HasColumnName("godfather_count").IsRequired();
        builder.Property(f => f.GodmotherCount).HasColumnName("godmother_count").IsRequired();
        builder.Property(f => f.RetreatId).HasColumnName("retreat_id").IsRequired();
        builder.Property(f => f.MemberLimit).HasColumnName("member_limit").IsRequired();

        builder.HasMany(f => f.Members).WithOne().HasForeignKey(r => r.FamilyId);
    }
}