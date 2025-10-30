using FluentValidation;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Tents.Create;

public sealed class CreateTentValidator : AbstractValidator<CreateTentCommand>
{
    public CreateTentValidator(ITentRepository tentRepo)
    {
        RuleFor(x => x.RetreatId).NotEmpty();

        RuleFor(x => x.Number)
            .NotEmpty()
            .Must(n => int.TryParse(n, out _))
            .WithMessage("Number deve ser numérico.");

        RuleFor(x => x.Category)
            .Must(c => c is TentCategory.Male or TentCategory.Female)
            .WithMessage("Categoria inválida. Use Male ou Female.");

        RuleFor(x => x.Capacity)
            .GreaterThan(0);

        RuleFor(x => x.Notes)
            .MaximumLength(200)
            .When(x => x.Notes is not null);

        // Unicidade: (RetreatId, Category, Number)
        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
            {
                if (!int.TryParse(cmd.Number, out var n)) return false;
                var exists = await tentRepo.ExistsNumberAsync(
                    cmd.RetreatId, cmd.Category, new TentNumber(n), ignoreId: null, ct);
                return !exists;
            })
            .WithMessage("Já existe barraca com este número para a mesma categoria neste retiro.");
    }
}