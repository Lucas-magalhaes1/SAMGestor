using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasKey(u => u.Id);

        builder.OwnsOne(u => u.Name, n =>
        {
            n.Property(p => p.Value).HasColumnName("name").HasMaxLength(120).IsRequired();
        });

        builder.OwnsOne(u => u.Email, e =>
        {
            e.Property(p => p.Value).HasColumnName("email").HasMaxLength(160).IsRequired();
            e.HasIndex(p => p.Value).IsUnique();
        });

        builder.OwnsOne(u => u.PasswordHash, p =>
        {
            p.Property(h => h.Value).HasColumnName("password_hash").HasMaxLength(200).IsRequired();
        });

        builder.Property(u => u.Phone).HasColumnName("phone").HasMaxLength(20).IsRequired();
        builder.Property(u => u.Role).HasColumnName("role").HasConversion<string>().IsRequired();
        builder.Property(u => u.Enabled).HasColumnName("enabled").IsRequired();
    }
}