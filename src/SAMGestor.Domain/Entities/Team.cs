using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class Team : Entity<Guid>
{
    private readonly List<TeamMember> _members = new();

    public FullName Name { get; private set; }
    public int MemberLimit { get; private set; }
    public Guid RetreatId { get; private set; }
    
    public string Description { get; private set; }
    
    public int MinMembers { get; private set; }
    
    public IReadOnlyCollection<TeamMember> Members => _members;

    private Team() { }

    public Team(FullName name,
        string description,
        int minMembers,
        int memberLimit,
        Guid retreatId)
    {
        Id = Guid.NewGuid();
        Name = name;
        Description = description.Trim();
        MinMembers = minMembers;
        MemberLimit = memberLimit;
        RetreatId = retreatId;
    }

    public void AddMember(Guid registrationId, TeamMemberRole role)
    {
        if (_members.Count >= MemberLimit) throw new InvalidOperationException();
        if (_members.Any(m => m.RegistrationId == registrationId)) throw new InvalidOperationException();
        _members.Add(new TeamMember(this, registrationId, role));
    }
}