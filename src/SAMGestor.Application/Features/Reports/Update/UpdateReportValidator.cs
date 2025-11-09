using FluentValidation;

namespace SAMGestor.Application.Features.Reports.Update;

public sealed class UpdateReportValidator : AbstractValidator<UpdateReportCommand>
{
    public UpdateReportValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(160);
        RuleFor(x => x.TemplateKey).MaximumLength(80).When(x => x.TemplateKey is not null);
    }
}