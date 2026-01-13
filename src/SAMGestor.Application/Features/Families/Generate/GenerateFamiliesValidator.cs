using FluentValidation;

namespace SAMGestor.Application.Features.Families.Generate;

public sealed class GenerateFamiliesValidator : AbstractValidator<GenerateFamiliesCommand>
{
    public GenerateFamiliesValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("RetreatId é obrigatório.");

        RuleFor(x => x.MembersPerFamily)
            .GreaterThanOrEqualTo(4).WithMessage("Número de membros por família deve ser no mínimo 4.")
            .Must(x => x % 2 == 0).WithMessage("Número de membros por família deve ser PAR para balanceamento de gênero.");
    }
}