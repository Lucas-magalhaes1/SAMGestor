using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Specifications;

public sealed class FeitoNotContemplableSpecification : ISpecification<Registration>
{
    public bool IsSatisfiedBy(Registration reg) => !reg.CompletedRetreat;
}
