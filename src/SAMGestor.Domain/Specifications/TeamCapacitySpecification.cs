using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Domain.Specifications;

public sealed class TeamCapacitySpecification : ISpecification<Team>
{
    private readonly ITeamRepository _teams;

    public TeamCapacitySpecification(ITeamRepository teams)
    {
        _teams = teams;
    }

    public bool IsSatisfiedBy(Team team)
    {
        var current = _teams.GetOccupancy(team.Id);
        return current < team.MemberLimit;
    }
}