using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.Specifications;

namespace SAMGestor.Domain.Specifications;

public sealed class FamilyStructureValidSpecification : ISpecification<Family>
{
    public bool IsSatisfiedBy(Family family)
    {
        return family.GodfatherCount >= 2 && family.GodmotherCount >= 2;
    }
}