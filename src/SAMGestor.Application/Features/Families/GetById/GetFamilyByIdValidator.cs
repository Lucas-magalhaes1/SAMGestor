using FluentValidation;

namespace SAMGestor.Application.Features.Families.GetById;

public sealed class GetFamilyByIdValidator : AbstractValidator<GetFamilyByIdQuery>
{
    public GetFamilyByIdValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty().WithMessage("RetreatId é obrigatório.");
        RuleFor(x => x.FamilyId).NotEmpty().WithMessage("FamilyId é obrigatório.");
    }
}