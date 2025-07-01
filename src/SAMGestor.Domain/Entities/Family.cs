
using SAMGestor.Domain.Commom;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class Family : Entity<Guid>
{
    private readonly List<Registration> _members = new();

    public FullName Name { get; private set; }
    public int GodfatherCount { get; private set; }
    public int GodmotherCount { get; private set; }
    public Guid RetreatId { get; private set; }
    public int MemberLimit { get; private set; }
    public IReadOnlyCollection<Registration> Members => _members;

    private Family() { }

    public Family(FullName name, int godfatherCount, int godmotherCount, Guid retreatId, int memberLimit)
    {
        Id = Guid.NewGuid();
        Name = name;
        GodfatherCount = godfatherCount;
        GodmotherCount = godmotherCount;
        RetreatId = retreatId;
        MemberLimit = memberLimit;
    }

    public void AddMember(Registration registration)
    {
        if (_members.Count >= MemberLimit) throw new InvalidOperationException();
        if (_members.Any(r => r.Id == registration.Id)) throw new InvalidOperationException();
        _members.Add(registration);
    }
}