using FluentValidation;
using SAMGestor.Application.Dtos.Auth;

namespace SAMGestor.Application.Dtos.Validators;

public class ConfirmEmailRequestValidator : AbstractValidator<ConfirmEmailRequest>
{
    public ConfirmEmailRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token é obrigatório.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Nova senha é obrigatória.")
            .MinimumLength(8).WithMessage("Senha deve ter pelo menos 8 caracteres.")
            .Matches("[A-Z]").WithMessage("Senha deve ter ao menos uma letra maiúscula.")
            .Matches("[a-z]").WithMessage("Senha deve ter ao menos uma letra minúscula.")
            .Matches("[0-9]").WithMessage("Senha deve ter ao menos um número.");
    }
}