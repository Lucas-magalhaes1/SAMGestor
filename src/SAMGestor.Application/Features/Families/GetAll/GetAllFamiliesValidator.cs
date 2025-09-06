using FluentValidation;

namespace SAMGestor.Application.Features.Families.GetAll;

public sealed class GetAllFamiliesValidator : AbstractValidator<GetAllFamiliesQuery>
{
    public GetAllFamiliesValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("RetreatId é obrigatório.");
    }
}