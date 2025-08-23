using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class BlockedCpfConfiguration : IEntityTypeConfiguration<BlockedCpf>
{
    public void Configure(EntityTypeBuilder<BlockedCpf> builder)
    {
        builder.ToTable("blocked_cpfs");
        builder.HasKey(b => b.Id);

        // >>> Remova o bloco OwnsOne e use converter:
        builder.Property(b => b.Cpf)
            .HasConversion(
                toProvider   => toProvider.Value,
                fromProvider => new CPF(fromProvider)
            )
            .HasColumnName("cpf")
            .HasMaxLength(11)
            .IsRequired();

        builder.HasIndex(b => b.Cpf).IsUnique();

        builder.Property(b => b.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();
    }
}