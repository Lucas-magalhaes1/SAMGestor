using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Domain.Entities;

public class TeamMember : Entity<Guid>
{
    public Guid RegistrationId { get; private set; }
    public Guid TeamId { get; private set; }
    public TeamMemberRole Role { get; private set; }

    private TeamMember() { }

    internal TeamMember(Team team, Guid registrationId, TeamMemberRole role)
    {
        Id = Guid.NewGuid();
        TeamId = team.Id;
        RegistrationId = registrationId;
        Role = role;
    }
}