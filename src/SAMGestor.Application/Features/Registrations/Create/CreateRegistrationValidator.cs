using FluentValidation;
using SAMGestor.Application.Features.Registrations.Create;

namespace SAMGestor.Application.Features.Registrations.Create;

public class CreateRegistrationValidator : AbstractValidator<CreateRegistrationCommand>
{
    public CreateRegistrationValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();

        RuleFor(x => x.Name).NotNull();
        RuleFor(x => x.Name.Value).NotEmpty().MaximumLength(120);

        RuleFor(x => x.Cpf).NotNull();

        RuleFor(x => x.Email).NotNull();

        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);

        RuleFor(x => x.BirthDate).LessThan(DateOnly.FromDateTime(DateTime.UtcNow));

        RuleFor(x => x.City).NotEmpty().MaximumLength(100);

        RuleFor(x => x.Region).NotEmpty().MaximumLength(50);
    }
}