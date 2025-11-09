using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class ReportInstanceConfiguration : IEntityTypeConfiguration<ReportInstance>
{
    public void Configure(EntityTypeBuilder<ReportInstance> builder)
    {
        builder.ToTable("report_instances");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ReportId)
            .HasColumnName("report_id")
            .IsRequired();

        builder.Property(x => x.Format)
            .HasColumnName("format")
            .HasConversion<string>() // salva como "Pdf" / "Xlsx"
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.GeneratedAt)
            .HasColumnName("generated_at")
            .IsRequired();

        builder.Property(x => x.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(160);

        builder.Property(x => x.StoragePath)
            .HasColumnName("storage_path")
            .HasMaxLength(400);

        builder.Property(x => x.FileSizeBytes)
            .HasColumnName("file_size_bytes");

        builder.HasOne<Report>()
            .WithMany()
            .HasForeignKey(x => x.ReportId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ReportId, x.GeneratedAt })
            .HasDatabaseName("ix_report_instances_report_date");
    }
}