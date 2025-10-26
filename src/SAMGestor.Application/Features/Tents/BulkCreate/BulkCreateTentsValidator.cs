using FluentValidation;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Tents.BulkCreate;

public sealed class BulkCreateTentsValidator : AbstractValidator<BulkCreateTentsCommand>
{
    public BulkCreateTentsValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();

        RuleFor(x => x.Items)
            .NotNull()
            .Must(list => list?.Count > 0)
            .WithMessage("Items não pode ser vazio.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.Number)
                .NotEmpty()
                .Must(n => int.TryParse(n, out _))
                .WithMessage("Number deve ser numérico.");

            item.RuleFor(i => i.Category)
                .IsInEnum()
                .Must(c => c is TentCategory.Male or TentCategory.Female);

            item.RuleFor(i => i.Capacity)
                .GreaterThan(0);

            item.RuleFor(i => i.Notes)
                .MaximumLength(200)
                .When(i => i.Notes is not null);
        });

        // Duplicado no payload (Category, Number)
        RuleFor(x => x.Items)
            .Must(items =>
            {
                if (items is null) return true;
                var set = new HashSet<(TentCategory cat, string num)>();
                foreach (var i in items)
                {
                    var key = (i.Category, i.Number.Trim());
                    if (!set.Add(key)) return false;
                }
                return true;
            })
            .WithMessage("Há barracas duplicadas no payload (mesma Category e Number).");
    }
}