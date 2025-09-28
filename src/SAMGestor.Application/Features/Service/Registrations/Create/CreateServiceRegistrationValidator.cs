using FluentValidation;

namespace SAMGestor.Application.Features.Service.Registrations.Create;

public class CreateServiceRegistrationValidator : AbstractValidator<CreateServiceRegistrationCommand>
{
    public CreateServiceRegistrationValidator()
    {
        RuleFor(x => x.RetreatId).NotEmpty();

        RuleFor(x => x.Name).NotNull();
        RuleFor(x => x.Name.Value).NotEmpty().MaximumLength(160);

        RuleFor(x => x.Cpf).NotNull();
        RuleFor(x => x.Email).NotNull();

        RuleFor(x => x.Phone).NotEmpty().MaximumLength(40);

        RuleFor(x => x.BirthDate)
            .LessThan(DateOnly.FromDateTime(DateTime.UtcNow));

        RuleFor(x => x.City).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Region).NotEmpty().MaximumLength(120);
    }
}