using FluentValidation;

namespace SAMGestor.Application.Features.Service.Roster.Update;

public sealed class UpdateServiceRosterValidator : AbstractValidator<UpdateServiceRosterCommand>
{
    public UpdateServiceRosterValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();
        RuleFor(x => x.Spaces).NotNull();
        RuleForEach(x => x.Spaces).ChildRules(space =>
        {
            space.RuleFor(s => s.SpaceId).NotEmpty();
            space.RuleFor(s => s.Members).NotNull();
            space.RuleForEach(s => s.Members).ChildRules(m =>
            {
                m.RuleFor(v => v.RegistrationId).NotEmpty();
                m.RuleFor(v => v.Position).GreaterThanOrEqualTo(0);
            });
        });
    }
}