using FluentValidation;

namespace SAMGestor.Application.Features.Families.UpdateGodparents;

public sealed class UpdateGodparentsValidator : AbstractValidator<UpdateGodparentsCommand>
{
    public UpdateGodparentsValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("RetreatId é obrigatório.");

        RuleFor(x => x.FamilyId)
            .NotEmpty().WithMessage("FamilyId é obrigatório.");

        RuleFor(x => x.PadrinhoIds)
            .NotNull().WithMessage("Lista de padrinhos não pode ser nula.");

        RuleFor(x => x.MadrinhaIds)
            .NotNull().WithMessage("Lista de madrinhas não pode ser nula.");
    }
}