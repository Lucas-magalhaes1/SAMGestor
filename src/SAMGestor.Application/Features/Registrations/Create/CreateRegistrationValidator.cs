using FluentValidation;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Registrations.Create;

public class CreateRegistrationValidator : AbstractValidator<CreateRegistrationCommand>
{
    public CreateRegistrationValidator()
    {
      
        RuleFor(x => x.RetreatId).NotEmpty();
        RuleFor(x => x.Name).NotNull();
        RuleFor(x => x.Name.Value).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Cpf).NotNull();
        RuleFor(x => x.Email).NotNull();
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.BirthDate).LessThan(DateOnly.FromDateTime(DateTime.UtcNow));
        RuleFor(x => x.City).NotEmpty().MaximumLength(80);
        
        RuleFor(x => x.MaritalStatus).IsInEnum();
        RuleFor(x => x.ShirtSize).IsInEnum();
        RuleFor(x => x.WeightKg).GreaterThan(0).LessThanOrEqualTo(400);
        RuleFor(x => x.HeightCm).GreaterThan(0).LessThanOrEqualTo(300);
        RuleFor(x => x.Profession).NotEmpty().MaximumLength(120);
        RuleFor(x => x.StreetAndNumber).NotEmpty().MaximumLength(160);
        RuleFor(x => x.Neighborhood).NotEmpty().MaximumLength(120);
        RuleFor(x => x.State).IsInEnum();
        
        RuleFor(x => x.Pregnancy).IsInEnum();
        When(x => x.Gender != Gender.Female, () =>
        {
            RuleFor(x => x.Pregnancy).Equal(PregnancyStatus.None)
                .WithMessage("Pregnancy must be 'None' when gender is not Female.");
        });
        
        RuleFor(x => x.NeighborPhone)
            .NotEmpty()
            .Must(IsPhoneDigits).WithMessage("NeighborPhone must have 10–11 digits.");

        RuleFor(x => x.RelativePhone)
            .NotEmpty()
            .Must(IsPhoneDigits).WithMessage("RelativePhone must have 10–11 digits.");

        When(x => !string.IsNullOrWhiteSpace(x.Whatsapp), () =>
        {
            RuleFor(x => x.Whatsapp!)
                .Must(IsPhoneDigits).WithMessage("Whatsapp must have 10–11 digits.");
        });

        RuleFor(x => x.FacebookUsername).MaximumLength(50);
        RuleFor(x => x.InstagramHandle).MaximumLength(50);
        RuleFor(x => x.FatherStatus).IsInEnum();
        RuleFor(x => x.MotherStatus).IsInEnum();

        RuleFor(x => x.HadFamilyLossLast6Months).NotNull();
        When(x => x.HadFamilyLossLast6Months, () =>
        {
            RuleFor(x => x.FamilyLossDetails).NotEmpty().MaximumLength(300);
        });

        RuleFor(x => x.HasRelativeOrFriendSubmitted).NotNull();
        When(x => x.HasRelativeOrFriendSubmitted, () =>
        {
            RuleFor(x => x.SubmitterRelationship).Must(r => r != RelationshipDegree.None)
                .WithMessage("Select at least one relationship.");
            RuleFor(x => x.SubmitterNames).NotEmpty().MaximumLength(200);
        });
        When(x => !x.HasRelativeOrFriendSubmitted, () =>
        {
            RuleFor(x => x.SubmitterRelationship).Equal(RelationshipDegree.None);
        });
        
        RuleFor(x => x.Religion).NotEmpty().MaximumLength(80);
        RuleFor(x => x.PreviousUncalledApplications).IsInEnum();
        RuleFor(x => x.RahaminVidaCompleted).IsInEnum();
        
        RuleFor(x => x.AlcoholUse).IsInEnum();
        RuleFor(x => x.Smoker).NotNull();
        RuleFor(x => x.UsesDrugs).NotNull();
        When(x => x.UsesDrugs, () =>
        {
            RuleFor(x => x.DrugUseFrequency).NotEmpty().MaximumLength(60);
        });

        RuleFor(x => x.HasAllergies).NotNull();
        When(x => x.HasAllergies, () =>
        {
            RuleFor(x => x.AllergiesDetails).NotEmpty().MaximumLength(300);
        });

        RuleFor(x => x.HasMedicalRestriction).NotNull();
        When(x => x.HasMedicalRestriction, () =>
        {
            RuleFor(x => x.MedicalRestrictionDetails).NotEmpty().MaximumLength(300);
        });

        RuleFor(x => x.TakesMedication).NotNull();
        When(x => x.TakesMedication, () =>
        {
            RuleFor(x => x.MedicationsDetails).NotEmpty().MaximumLength(300);
        });

        RuleFor(x => x.PhysicalLimitationDetails).MaximumLength(300);
        RuleFor(x => x.RecentSurgeryOrProcedureDetails).MaximumLength(300);
        
        RuleFor(x => x.TermsAccepted).Equal(true)
            .WithMessage("Terms must be accepted.");
        RuleFor(x => x.TermsVersion).NotEmpty().MaximumLength(50);
    }

    static bool IsPhoneDigits(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return false;
        var digits = new string(v.Where(char.IsDigit).ToArray());
        return digits.Length is >= 10 and <= 11;
    }
}
