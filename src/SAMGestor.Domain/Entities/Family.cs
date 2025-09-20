using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class Family : Entity<Guid>
{
    private readonly List<FamilyMember> _members = new();
    public FamilyName Name { get; private set; } 
    public Guid     RetreatId  { get; private set; }
    public bool IsLocked { get; private set; } 
    public int Capacity   { get; private set; }
    
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

    public Family(FamilyName name, Guid retreatId, int capacity)
    {
        Id        = Guid.NewGuid();
        Name      = name;
        RetreatId = retreatId;
        Capacity  = capacity;
    }

    public void Rename(FamilyName name) => Name = name;

    public void SetCapacity(int capacity)
    {
        if (capacity <= 0) throw new ArgumentException(nameof(capacity));
        Capacity = capacity;
    }
    
    public void Lock()   => IsLocked = true;
    public void Unlock() => IsLocked = false;
    
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