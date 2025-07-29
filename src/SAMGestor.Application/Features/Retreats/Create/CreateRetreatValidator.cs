using FluentValidation;

namespace SAMGestor.Application.Features.Retreats.Create;

public class CreateRetreatValidator : AbstractValidator<CreateRetreatCommand>
{
    public CreateRetreatValidator()
    {
        // Name (FullName VO)
        RuleFor(x => x.Name)
            .NotNull().WithMessage("Name is required.");

        RuleFor(x => x.Name.Value)
            .NotEmpty().WithMessage("Name cannot be empty.")
            .MaximumLength(120).WithMessage("Name cannot exceed 120 characters.")
            .When(x => x.Name is not null);

        // Edition
        RuleFor(x => x.Edition)
            .NotEmpty().WithMessage("Edition is required.")
            .MaximumLength(50).WithMessage("Edition cannot exceed 50 characters.");

        // Theme
        RuleFor(x => x.Theme)
            .NotEmpty().WithMessage("Theme is required.")
            .MaximumLength(120).WithMessage("Theme cannot exceed 120 characters.");

        // Dates
        RuleFor(x => x.StartDate)
            .NotEmpty().WithMessage("StartDate is required.");

        RuleFor(x => x.EndDate)
            .NotEmpty().WithMessage("EndDate is required.")
            .GreaterThan(x => x.StartDate)
            .WithMessage("EndDate must be after StartDate.");

        RuleFor(x => x.RegistrationStart)
            .NotEmpty().WithMessage("RegistrationStart is required.");

        RuleFor(x => x.RegistrationEnd)
            .NotEmpty().WithMessage("RegistrationEnd is required.")
            .GreaterThan(x => x.RegistrationStart)
            .WithMessage("RegistrationEnd must be after RegistrationStart.");

        // Slots
        RuleFor(x => x.MaleSlots)
            .GreaterThanOrEqualTo(0).WithMessage("MaleSlots must be greater than or equal to 0.");

        RuleFor(x => x.FemaleSlots)
            .GreaterThanOrEqualTo(0).WithMessage("FemaleSlots must be greater than or equal to 0.");

        // FeeFazer
        RuleFor(x => x.FeeFazer)
            .NotNull().WithMessage("FeeFazer is required.");

        RuleFor(x => x.FeeFazer.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("FeeFazer amount must be >= 0.")
            .When(x => x.FeeFazer is not null);

        RuleFor(x => x.FeeFazer.Currency)
            .NotEmpty().WithMessage("FeeFazer currency is required.")
            .When(x => x.FeeFazer is not null);

        // FeeServir
        RuleFor(x => x.FeeServir)
            .NotNull().WithMessage("FeeServir is required.");

        RuleFor(x => x.FeeServir.Amount)
            .GreaterThanOrEqualTo(0).WithMessage("FeeServir amount must be >= 0.")
            .When(x => x.FeeServir is not null);

        RuleFor(x => x.FeeServir.Currency)
            .NotEmpty().WithMessage("FeeServir currency is required.")
            .When(x => x.FeeServir is not null);

        RuleFor(x => x.WestRegionPct)
            .NotNull().WithMessage("WestRegionPct is required.");

        RuleFor(x => x.OtherRegionPct)
            .NotNull().WithMessage("OtherRegionPct is required.");

        RuleFor(x => x)
            .Must(cmd => cmd.WestRegionPct?.Value + cmd.OtherRegionPct?.Value == 100)
            .WithMessage("WestRegionPct + OtherRegionPct must equal 100.");
    }
}
