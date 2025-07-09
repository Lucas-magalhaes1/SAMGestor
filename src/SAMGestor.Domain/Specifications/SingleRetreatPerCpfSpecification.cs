using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Domain.Specifications;

public sealed class SingleRetreatPerCpfSpecification : ISpecification<Registration>
{
    private readonly IRegistrationRepository _repo;
    public SingleRetreatPerCpfSpecification(IRegistrationRepository repo) => _repo = repo;

    public bool IsSatisfiedBy(Registration reg)
    {
        return !_repo.GetAllByCpf(reg.Cpf)
            .Any(r => r.CompletedRetreat && r.Id != reg.Id);
    }
}