using FluentValidation;

namespace SAMGestor.Application.Features.Families.Generate;

public sealed class GenerateFamiliesValidator : AbstractValidator<GenerateFamiliesCommand>
{
    private const int DefaultCapacity = 4;

    public GenerateFamiliesValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("RetreatId é obrigatório.");

        RuleFor(x => x.Capacity)
            .Must(c => c is null || c > 0)
            .WithMessage("Capacity (se informado) deve ser > 0.");
    }
}