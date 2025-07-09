using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Domain.Specifications;

public sealed class UniqueCpfSpecification : ISpecification<Registration>
{
    private readonly IRegistrationRepository _repository;

    public UniqueCpfSpecification(IRegistrationRepository repository)
    {
        _repository = repository;
    }

    public bool IsSatisfiedBy(Registration registration)
    {
        var existing = _repository.GetByCpfAndRetreat(registration.Cpf, registration.RetreatId);
        return existing == null || existing.Id == registration.Id;
    }
}