using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.Specifications;

public sealed class UniqueCoordinatorSpecification : ISpecification<Team>
{
    private readonly ITeamRepository _teams;

    public UniqueCoordinatorSpecification(ITeamRepository teams)
    {
        _teams = teams;
    }

    public bool IsSatisfiedBy(Team team)
    {
        return !_teams.HasRole(team.Id, TeamMemberRole.Coordinator)
               || team.Members.Count(m => m.Role == TeamMemberRole.Coordinator) == 1;
    }
}