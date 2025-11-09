using FluentValidation;

namespace SAMGestor.Application.Features.Reports.Create;

public sealed class CreateReportValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.TemplateKey).NotEmpty().MaximumLength(80);
    }
}