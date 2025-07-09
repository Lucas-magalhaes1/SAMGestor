using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Domain.Specifications;

public sealed class CpfNotBlockedSpecification : ISpecification<Registration>
{
    private readonly IRegistrationRepository _repository;

    public CpfNotBlockedSpecification(IRegistrationRepository repository)
    {
        _repository = repository;
    }

    public bool IsSatisfiedBy(Registration registration)
    {
        return !_repository.IsCpfBlocked(registration.Cpf);
    }
}