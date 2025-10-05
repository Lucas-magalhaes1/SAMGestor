using FluentValidation;

namespace SAMGestor.Application.Features.Service.Spaces.Create;

public sealed class CreateServiceSpaceValidator : AbstractValidator<CreateServiceSpaceCommand>
{
    public CreateServiceSpaceValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Description).MaximumLength(512).When(x => x.Description != null);

        RuleFor(x => x.MaxPeople).GreaterThan(0);
        RuleFor(x => x.MinPeople).GreaterThanOrEqualTo(0);
        RuleFor(x => x).Must(x => x.MinPeople <= x.MaxPeople)
            .WithMessage("MinPeople must be <= MaxPeople");
    }
}