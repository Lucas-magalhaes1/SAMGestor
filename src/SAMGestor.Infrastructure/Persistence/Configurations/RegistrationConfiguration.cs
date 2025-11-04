using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Infrastructure.Persistence.Configurations;

public class RegistrationConfiguration : IEntityTypeConfiguration<Registration>
{
    public void Configure(EntityTypeBuilder<Registration> builder)
    {
        builder.ToTable("registrations");
        
        builder.HasKey(r => r.Id);
        
        builder.OwnsOne(r => r.Name, n =>
        {
            n.Property(p => p.Value)
             .HasColumnName("name")
             .HasMaxLength(120)
             .IsRequired();
        });

        builder.Property(r => r.Cpf)
            .HasConversion(
                toProvider   => toProvider.Value,
                fromProvider => new CPF(fromProvider)
            )
            .HasColumnName("cpf")
            .HasMaxLength(11)
            .IsRequired();

        builder.Property(r => r.Email)
            .HasConversion(
                toProvider   => toProvider.Value,
                fromProvider => new EmailAddress(fromProvider)
            )
            .HasColumnName("email")
            .HasMaxLength(160)
            .IsRequired();

        builder.OwnsOne(r => r.PhotoUrl, u =>
        {
            u.Property(p => p.Value)
             .HasColumnName("photo_url")
             .HasMaxLength(300);
        });
       
        builder.Property(r => r.Gender)
            .HasColumnName("gender")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.RetreatId)
            .HasColumnName("retreat_id")
            .IsRequired();

        builder.HasOne<Retreat>()
            .WithMany()
            .HasForeignKey(r => r.RetreatId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Phone)
            .HasColumnName("phone")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.BirthDate)
            .HasColumnName("birth_date")
            .IsRequired();

        builder.Property(r => r.City)
            .HasColumnName("city")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.Enabled)
            .HasColumnName("enabled")
            .IsRequired();

        builder.Property(r => r.CompletedRetreat)
            .HasColumnName("completed_retreat")
            .IsRequired();

        builder.Property(r => r.RegistrationDate)
            .HasColumnName("registration_date")
            .IsRequired();

        builder.Property(r => r.TentId).HasColumnName("tent_id");
        
        builder.Property(r => r.TeamId).HasColumnName("team_id");

        builder.HasIndex(r => new { r.RetreatId, r.Status, r.Gender });
        
        builder.HasIndex(r => new { r.RetreatId, r.Cpf }).IsUnique();
        
        builder.HasIndex(r => new { r.RetreatId, r.Email }).IsUnique();
        builder.HasIndex(r => r.Cpf);
        
        builder.HasIndex(r => r.Email);
        
        builder.Property(r => r.MaritalStatus)
            .HasColumnName("marital_status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.Pregnancy)
            .HasColumnName("pregnancy")
            .HasConversion<string>()
            .HasDefaultValue(PregnancyStatus.None)
            .IsRequired();

        builder.Property(r => r.ShirtSize)
            .HasColumnName("shirt_size")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.WeightKg)
            .HasColumnName("weight_kg")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(r => r.HeightCm)
            .HasColumnName("height_cm")
            .HasPrecision(5, 2)
            .IsRequired();

        builder.Property(r => r.Profession)
            .HasColumnName("profession")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(r => r.StreetAndNumber)
            .HasColumnName("street_and_number")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(r => r.Neighborhood)
            .HasColumnName("neighborhood")
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(r => r.State)
            .HasColumnName("state")
            .HasConversion<string>()
            .IsRequired();
       
        builder.Property(r => r.Whatsapp)
            .HasColumnName("whatsapp")
            .HasMaxLength(20); 

        builder.Property(r => r.FacebookUsername)
            .HasColumnName("facebook_username")
            .HasMaxLength(50); 

        builder.Property(r => r.InstagramHandle)
            .HasColumnName("instagram_handle")
            .HasMaxLength(50); 

        builder.Property(r => r.NeighborPhone)
            .HasColumnName("neighbor_phone")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(r => r.RelativePhone)
            .HasColumnName("relative_phone")
            .HasMaxLength(20)
            .IsRequired();
        
        builder.Property(r => r.TermsAccepted)
            .HasColumnName("terms_accepted")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(r => r.TermsAcceptedAt)
            .HasColumnName("terms_accepted_at")
            .IsRequired();

        builder.Property(r => r.TermsVersion)
            .HasColumnName("terms_version")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(r => r.MarketingOptIn)
            .HasColumnName("marketing_opt_in")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(r => r.MarketingOptInAt)
            .HasColumnName("marketing_opt_in_at")
            .IsRequired();

        builder.Property(r => r.ClientIp)
            .HasColumnName("client_ip")
            .HasMaxLength(45); 
        
        builder.Property(r => r.UserAgent)
            .HasColumnName("user_agent")
            .HasMaxLength(512); 
        
        builder.Property(r => r.FatherStatus)
            .HasColumnName("father_status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.FatherName)
            .HasColumnName("father_name")
            .HasMaxLength(120); 

        builder.Property(r => r.FatherPhone)
            .HasColumnName("father_phone")
            .HasMaxLength(20);

        builder.Property(r => r.MotherStatus)
            .HasColumnName("mother_status")
            .HasConversion<string>()
            .IsRequired();

        builder.Property(r => r.MotherName)
            .HasColumnName("mother_name")
            .HasMaxLength(120);

        builder.Property(r => r.MotherPhone)
            .HasColumnName("mother_phone")
            .HasMaxLength(20); 

        builder.Property(r => r.HadFamilyLossLast6Months)
            .HasColumnName("had_family_loss_last6m")
            .IsRequired();

        builder.Property(r => r.FamilyLossDetails)
            .HasColumnName("family_loss_details")
            .HasMaxLength(300); 

        builder.Property(r => r.HasRelativeOrFriendSubmitted)
            .HasColumnName("has_relative_or_friend_submitted")
            .IsRequired();

        builder.Property(r => r.SubmitterRelationship)
            .HasColumnName("submitter_relationship")
            .HasConversion<string>()
            .HasDefaultValue(RelationshipDegree.None)
            .IsRequired();

        builder.Property(r => r.SubmitterNames)
            .HasColumnName("submitter_names")
            .HasMaxLength(200); 
        
        builder.Property(r => r.Religion)
            .HasColumnName("religion")
            .HasMaxLength(80)
            .IsRequired();

        builder.Property(r => r.PreviousUncalledApplications)
            .HasColumnName("prev_uncalled_applications")
            .HasConversion<string>()
            .HasDefaultValue(RahaminAttempt.None)
            .IsRequired();

        builder.Property(r => r.RahaminVidaCompleted)
            .HasColumnName("rahamin_vida_completed")
            .HasConversion<string>()
            .HasDefaultValue(RahaminVidaEdition.None)
            .IsRequired();
       
        builder.Property(r => r.AlcoholUse)
            .HasColumnName("alcohol_use")
            .HasConversion<string>()
            .HasDefaultValue(AlcoholUsePattern.None)
            .IsRequired();

        builder.Property(r => r.Smoker)
            .HasColumnName("smoker")
            .IsRequired();

        builder.Property(r => r.UsesDrugs)
            .HasColumnName("uses_drugs")
            .IsRequired();

        builder.Property(r => r.DrugUseFrequency)
            .HasColumnName("drug_use_frequency")
            .HasMaxLength(60); 

        builder.Property(r => r.HasAllergies)
            .HasColumnName("has_allergies")
            .IsRequired();

        builder.Property(r => r.AllergiesDetails)
            .HasColumnName("allergies_details")
            .HasMaxLength(300); 

        builder.Property(r => r.HasMedicalRestriction)
            .HasColumnName("has_medical_restriction")
            .IsRequired();

        builder.Property(r => r.MedicalRestrictionDetails)
            .HasColumnName("medical_restriction_details")
            .HasMaxLength(300); 

        builder.Property(r => r.TakesMedication)
            .HasColumnName("takes_medication")
            .IsRequired();

        builder.Property(r => r.MedicationsDetails)
            .HasColumnName("medications_details")
            .HasMaxLength(300); 

        builder.Property(r => r.PhysicalLimitationDetails)
            .HasColumnName("physical_limitation_details")
            .HasMaxLength(300); 

        builder.Property(r => r.RecentSurgeryOrProcedureDetails)
            .HasColumnName("recent_surgery_or_procedure_details")
            .HasMaxLength(300); 
        
        builder.Property(r => r.PhotoStorageKey)
            .HasColumnName("photo_storage_key")
            .HasMaxLength(300); 

        builder.Property(r => r.PhotoContentType)
            .HasColumnName("photo_content_type")
            .HasMaxLength(60); 
        builder.Property(r => r.PhotoSizeBytes)
            .HasColumnName("photo_size_bytes"); 

        builder.Property(r => r.PhotoUploadedAt)
            .HasColumnName("photo_uploaded_at"); 

        builder.Property(r => r.IdDocumentType)
            .HasColumnName("id_document_type")
            .HasConversion<string>(); 

        builder.Property(r => r.IdDocumentNumber)
            .HasColumnName("id_document_number")
            .HasMaxLength(50); 

        builder.Property(r => r.IdDocumentStorageKey)
            .HasColumnName("id_document_storage_key")
            .HasMaxLength(300); 

        builder.OwnsOne(r => r.IdDocumentUrl, u =>
        {
            u.Property(p => p.Value)
             .HasColumnName("id_document_url")
             .HasMaxLength(300);
        });

        builder.Property(r => r.IdDocumentContentType)
            .HasColumnName("id_document_content_type")
            .HasMaxLength(60); 

        builder.Property(r => r.IdDocumentSizeBytes)
            .HasColumnName("id_document_size_bytes"); 

        builder.Property(r => r.IdDocumentUploadedAt)
            .HasColumnName("id_document_uploaded_at"); 
    }
}
