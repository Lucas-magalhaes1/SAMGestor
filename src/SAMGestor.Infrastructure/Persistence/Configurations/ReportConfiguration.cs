using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("reports");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasColumnName("title")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(x => x.TemplateKey)
            .HasColumnName("template_key")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(x => x.RetreatId)
            .HasColumnName("retreat_id");

        builder.Property(x => x.DateCreation)
            .HasColumnName("date_creation")
            .IsRequired();

        builder.Property(x => x.LastUpdate)
            .HasColumnName("last_update");

        builder.Property(x => x.CreatedByUserId)
            .HasColumnName("created_by_user_id");

        builder.Property(x => x.DefaultParamsJson)
            .HasColumnName("default_params_json")
            .HasMaxLength(4000);
        
        builder.HasOne<Retreat>()
            .WithMany()
            .HasForeignKey(x => x.RetreatId)
            .OnDelete(DeleteBehavior.Restrict);
        
        builder.HasIndex(x => new { x.RetreatId, x.TemplateKey })
            .HasDatabaseName("ix_reports_retreat_template");

        builder.HasIndex(x => x.DateCreation)
            .HasDatabaseName("ix_reports_date_creation");
    }
}