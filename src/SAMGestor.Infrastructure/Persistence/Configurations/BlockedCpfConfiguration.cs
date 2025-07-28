using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class BlockedCpfConfiguration : IEntityTypeConfiguration<BlockedCpf>
{
    public void Configure(EntityTypeBuilder<BlockedCpf> builder)
    {
        builder.ToTable("blocked_cpfs");
        builder.HasKey(b => b.Id);

        builder.OwnsOne(b => b.Cpf, c =>
        {
            c.Property(p => p.Value).HasColumnName("cpf").HasMaxLength(11).IsRequired();
            
            c.HasIndex(p => p.Value).IsUnique();
        });

        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}