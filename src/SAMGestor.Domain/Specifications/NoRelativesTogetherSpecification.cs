using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Domain.Specifications;

public sealed class NoRelativesTogetherSpecification : ISpecification<Family>
{
    private readonly IRelationshipService _relationship;

    public NoRelativesTogetherSpecification(IRelationshipService relationship)
    {
        _relationship = relationship;
    }

    public bool IsSatisfiedBy(Family family)
    {
        var members = family.Members.ToList();

        for (var i = 0; i < members.Count; i++)
        {
            for (var j = i + 1; j < members.Count; j++)
            {
                if (_relationship.AreSpouses(members[i].Id, members[j].Id) ||
                    _relationship.AreDirectRelatives(members[i].Id, members[j].Id))
                    return false;
            }
        }
        return true;
    }
}