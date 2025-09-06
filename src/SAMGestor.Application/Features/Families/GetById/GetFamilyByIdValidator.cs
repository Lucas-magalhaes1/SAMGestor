using FluentValidation;

namespace SAMGestor.Application.Features.Families.GetById;

public sealed class GetFamilyByIdValidator : AbstractValidator<GetFamilyByIdQuery>
{
    public GetFamilyByIdValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();
        RuleFor(x => x.FamilyId).NotEmpty();
    }
}