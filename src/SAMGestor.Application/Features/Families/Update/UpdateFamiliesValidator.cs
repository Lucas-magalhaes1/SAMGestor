using FluentValidation;

namespace SAMGestor.Application.Features.Families.Update;

public sealed class UpdateFamiliesValidator : AbstractValidator<UpdateFamiliesCommand>
{
    public UpdateFamiliesValidator()
    {
        RuleFor(x => x.RetreatId)
            .NotEmpty().WithMessage("RetreatId é obrigatório.");

        RuleFor(x => x.Version)
            .GreaterThanOrEqualTo(0).WithMessage("Version deve ser >= 0.");

        RuleFor(x => x.Families)
            .NotNull().WithMessage("Lista de famílias não pode ser nula.");

        RuleForEach(x => x.Families).ChildRules(f =>
        {
            f.RuleFor(x => x.FamilyId)
                .NotEmpty().WithMessage("FamilyId é obrigatório.");

            f.RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Nome da família é obrigatório.")
                .MaximumLength(120).WithMessage("Nome não pode ter mais de 120 caracteres.");

            f.RuleFor(x => x.ColorName)
                .NotEmpty().WithMessage("Nome da cor é obrigatório.");
            
            f.RuleFor(x => x.Members)
                .NotNull().WithMessage("Lista de membros não pode ser nula.");

            f.RuleFor(x => x.PadrinhoIds)
                .NotNull().WithMessage("Lista de padrinhos não pode ser nula.")
                .Must(ids => ids.Count <= 2).WithMessage("Máximo 2 padrinhos por família.");
            
            f.RuleFor(x => x.Capacity)
                .GreaterThanOrEqualTo(4).WithMessage("Capacidade mínima é 4 membros.");

            f.RuleFor(x => x.MadrinhaIds)
                .NotNull().WithMessage("Lista de madrinhas não pode ser nula.")
                .Must(ids => ids.Count <= 2).WithMessage("Máximo 2 madrinhas por família.");

         
            f.RuleFor(x => x.Members.Select(m => m.Position))
                .Must(pos => pos.Distinct().Count() == pos.Count())
                .WithMessage("Positions devem ser únicos dentro da mesma família.");

           
            f.RuleFor(x => x.Members.Select(m => m.RegistrationId))
                .Must(ids => ids.Distinct().Count() == ids.Count())
                .WithMessage("Não pode repetir o mesmo membro dentro da mesma família.");
        });
        
        RuleFor(cmd => cmd.Families.SelectMany(f => f.Members.Select(m => m.RegistrationId)))
            .Must(all =>
            {
                var arr = all.ToArray();
                return arr.Distinct().Count() == arr.Length;
            })
            .WithMessage("Um participante não pode estar em múltiplas famílias simultaneamente.");
    }
}
