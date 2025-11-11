using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

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

        builder.Property(u => u.EmailConfirmed).HasColumnName("email_confirmed").IsRequired();
        builder.Property(u => u.EmailConfirmedAt).HasColumnName("email_confirmed_at");
        builder.Property(u => u.FailedAccessCount).HasColumnName("failed_access_count").HasDefaultValue(0).IsRequired();
        builder.Property(u => u.LockoutEndAt).HasColumnName("lockout_end_at");
        builder.Property(u => u.LastLoginAt).HasColumnName("last_login_at");

        // ===== Navegações usando a PROPRIEDADE =====
        builder
            .HasMany(u => u.RefreshTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(u => u.EmailConfirmationTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(u => u.PasswordResetTokens)
            .WithOne(t => t.User)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // ===== Dizer ao EF que o acesso é por backing field =====
        builder.Navigation(u => u.RefreshTokens)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(u => u.EmailConfirmationTokens)
               .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(u => u.PasswordResetTokens)
               .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
