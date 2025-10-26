using FluentValidation;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Tents.Update;

public sealed class UpdateTentValidator : AbstractValidator<UpdateTentCommand>
{
    public UpdateTentValidator(ITentRepository tentRepo)
    {
        RuleFor(x => x.RetreatId).NotEmpty();
        RuleFor(x => x.TentId).NotEmpty();

        RuleFor(x => x.Number)
            .NotEmpty()
            .Must(n => int.TryParse(n, out _))
            .WithMessage("Number deve ser numérico.");

        RuleFor(x => x.Category)
            .IsInEnum()
            .Must(c => c is TentCategory.Male or TentCategory.Female);

        RuleFor(x => x.Capacity)
            .GreaterThan(0);

        RuleFor(x => x.Notes)
            .MaximumLength(200)
            .When(x => x.Notes is not null);

        // unicidade por (RetreatId, Category, Number), ignorando a própria barraca
        RuleFor(x => x)
            .MustAsync(async (cmd, ct) =>
            {
                if (!int.TryParse(cmd.Number, out var n)) return false;
                var exists = await tentRepo.ExistsNumberAsync(
                    cmd.RetreatId, cmd.Category, new TentNumber(n), ignoreId: cmd.TentId, ct);
                return !exists;
            })
            .WithMessage("Já existe barraca com este número para a mesma categoria neste retiro.");
    }
}