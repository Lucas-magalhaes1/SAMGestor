using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class Family : Entity<Guid>
{
    private readonly List<FamilyMember> _members = new();
    
    public FamilyName Name { get; private set; } 
    public Guid RetreatId  { get; private set; }
    public bool IsLocked { get; private set; } 
    public int Capacity   { get; private set; }
    public FamilyColor Color { get; private set; }
    public string?         GroupLink            { get; private set; }
    public string?         GroupExternalId      { get; private set; }
    public DateTimeOffset? GroupCreatedAt       { get; private set; }
    public string?         GroupChannel         { get; private set; }
    public DateTimeOffset? GroupLastNotifiedAt  { get; private set; }
    public GroupStatus     GroupStatus          { get; private set; } = GroupStatus.None;
    public int             GroupVersion         { get; private set; } = 0;
    
    public bool IsComplete => _members.Count >= Capacity;
    
    public IReadOnlyCollection<FamilyMember> Members => _members;

    private Family() { }

    public Family(FamilyName name, Guid retreatId, int capacity, FamilyColor color)
    {
        Id        = Guid.NewGuid();
        Name      = name ?? throw new ArgumentNullException(nameof(name));
        RetreatId = retreatId;
        Capacity  = capacity;
        Color     = color ?? throw new ArgumentNullException(nameof(color));
    }

    public void Rename(FamilyName name) => Name = name ?? throw new ArgumentNullException(nameof(name));

    public void SetCapacity(int capacity)
    {
        if (capacity < 2)
            throw new ArgumentException("Capacidade mínima de uma família é 2 membros.", nameof(capacity));
        Capacity = capacity;
    }

    public void ChangeColor(FamilyColor color)
    {
        Color = color ?? throw new ArgumentNullException(nameof(color));
    }
    
    public void Lock()   => IsLocked = true;
    public void Unlock() => IsLocked = false;
    
 
    public bool HasExactlyTwoPadrinhos() => _members.Count(m => m.IsPadrinho) == 2;
    
    public bool HasExactlyTwoMadrinhas() => _members.Count(m => m.IsMadrinha) == 2;
    
    public bool HasValidGodparentsComposition() => 
        HasExactlyTwoPadrinhos() && HasExactlyTwoMadrinhas();
    
    public decimal GetMalePercentage()
    {
        if (_members.Count == 0) return 0;
        return 0; 
    }

    public bool HasEvenMemberCount() => _members.Count % 2 == 0;
    
    public bool HasMinimumMembers() => _members.Count >= 4;
    
    public void SetGroup(string link, string? externalId, string channel, DateTimeOffset now)
    {
        GroupLink       = link;
        GroupExternalId = externalId;
        GroupChannel    = channel;
        GroupCreatedAt  = now;
    }

    public void ClearGroup()
    {
        GroupLink           = null;
        GroupExternalId     = null;
        GroupChannel        = null;
        GroupCreatedAt      = null;
        GroupLastNotifiedAt = null;
        GroupStatus         = GroupStatus.None;
        GroupVersion++;
    }

    public void MarkGroupNotified(DateTimeOffset when) => GroupLastNotifiedAt = when;

    public void MarkGroupCreating()
    {
        GroupStatus  = GroupStatus.Creating;
        GroupVersion++;
    }

    public void MarkGroupActive(string link, string? externalId, string channel, DateTimeOffset createdAt, DateTimeOffset? notifiedAt)
    {
        SetGroup(link, externalId, channel, createdAt);
        GroupStatus         = GroupStatus.Active;
        GroupLastNotifiedAt = notifiedAt ?? GroupLastNotifiedAt;
    }

    public void MarkGroupFailed()
    {
        GroupStatus = GroupStatus.Failed;
    }
}
