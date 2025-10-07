using FluentValidation;

namespace SAMGestor.Application.Features.Service.Spaces.BulkCapacity;

public sealed class UpdateServiceSpacesCapacityValidator : AbstractValidator<UpdateServiceSpacesCapacityCommand>
{
    public UpdateServiceSpacesCapacityValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();

        When(x => x.ApplyToAll, () =>
        {
            RuleFor(x => x.MinPeople).NotNull().GreaterThanOrEqualTo(0);
            RuleFor(x => x.MaxPeople).NotNull().GreaterThan(0);
            RuleFor(x => x).Must(x => x.MinPeople! <= x.MaxPeople!)
                .WithMessage("MinPeople must be <= MaxPeople");
        });

        When(x => !x.ApplyToAll, () =>
        {
            RuleFor(x => x.Items).NotNull().Must(i => i!.Count > 0)
                .WithMessage("Items deve conter ao menos um espaço.");
            RuleForEach(x => x.Items!).ChildRules(c =>
            {
                c.RuleFor(i => i.MinPeople).GreaterThanOrEqualTo(0);
                c.RuleFor(i => i.MaxPeople).GreaterThan(0);
                c.RuleFor(i => i).Must(i => i.MinPeople <= i.MaxPeople)
                    .WithMessage("MinPeople must be <= MaxPeople");
            });
        });
    }
}