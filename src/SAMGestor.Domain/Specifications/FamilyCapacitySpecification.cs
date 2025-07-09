using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Specifications;

public sealed class FamilyCapacitySpecification : ISpecification<Family>
{
    public bool IsSatisfiedBy(Family family)
    {
        return family.Members.Count < family.MemberLimit;
    }
}