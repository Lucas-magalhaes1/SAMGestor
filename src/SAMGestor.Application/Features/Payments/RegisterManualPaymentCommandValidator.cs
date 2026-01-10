using FluentValidation;

namespace SAMGestor.Application.Features.Payments;

public sealed class RegisterManualPaymentCommandValidator : AbstractValidator<RegisterManualPaymentCommand>
{
    public RegisterManualPaymentCommandValidator()
    {
        RuleFor(x => x.RegistrationId)
            .NotEmpty().WithMessage("ID da inscrição é obrigatório");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("Método de pagamento é obrigatório")
            .Must(m => new[] { "Cash", "BankTransfer", "Check", "Other" }.Contains(m))
            .WithMessage("Método inválido. Use: Cash, BankTransfer, Check ou Other");

        RuleFor(x => x.PaymentDate)
            .NotEmpty().WithMessage("Data do pagamento é obrigatória")
            .LessThanOrEqualTo(DateTime.UtcNow.Date).WithMessage("Data não pode ser futura");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Valor deve ser maior que zero");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("Comprovante é obrigatório");

        RuleFor(x => x.FileSizeBytes)
            .GreaterThan(0).WithMessage("Arquivo vazio")
            .LessThanOrEqualTo(5 * 1024 * 1024).WithMessage("Arquivo muito grande (máx 5MB)");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Observações não podem exceder 1000 caracteres");
    }
}