using FluentValidation;
using System.Linq;

namespace SAMGestor.Application.Features.Families.Update;

public sealed class UpdateFamiliesValidator : AbstractValidator<UpdateFamiliesCommand>
{
    public UpdateFamiliesValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();
        RuleFor(x => x.Version).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Families).NotNull();

        RuleForEach(x => x.Families).ChildRules(f =>
        {
            f.RuleFor(x => x.FamilyId).NotEmpty();
            f.RuleFor(x => x.Name).NotEmpty();
            f.RuleFor(x => x.Capacity).GreaterThan(0);
            f.RuleFor(x => x.Members).NotNull();

            f.RuleFor(x => x.Members.Select(m => m.Position))
                .Must(pos => pos.Distinct().Count() == pos.Count())
                .WithMessage("Positions devem ser únicos por família.");

            f.RuleFor(x => x.Members.Select(m => m.RegistrationId))
                .Must(ids => ids.Distinct().Count() == ids.Count())
                .WithMessage("Não repita o mesmo Registration dentro da mesma família.");
        });
        
        RuleFor(cmd => cmd.Families
                .SelectMany(f => f.Members.Select(m => m.RegistrationId)))
            .Must(all =>
            {
                var arr = all.ToArray();
                return arr.Distinct().Count() == arr.Length;
            })
            .WithMessage("Um participante não pode estar em duas famílias no snapshot.");
    }
}