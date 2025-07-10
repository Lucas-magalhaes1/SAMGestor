using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Domain.Specifications;

public sealed class TeamMemberRoleRequiredSpecification : ISpecification<TeamMember>
{
    public bool IsSatisfiedBy(TeamMember member)
    {
        return Enum.IsDefined(typeof(TeamMemberRole), member.Role);
    }
}