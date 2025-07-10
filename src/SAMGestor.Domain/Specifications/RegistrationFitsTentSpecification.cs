using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Domain.Specifications;

public sealed class RegistrationFitsTentSpecification : ISpecification<Registration>
{
    private readonly Tent _tent;

    public RegistrationFitsTentSpecification(Tent tent)
    {
        _tent = tent;
    }

    public bool IsSatisfiedBy(Registration registration)
    {
        return (_tent.Category, registration.Gender) switch
        {
            (TentCategory.Male,   Gender.Male)   => true,
            (TentCategory.Female, Gender.Female) => true,
            _                                     => false
        };
    }
}