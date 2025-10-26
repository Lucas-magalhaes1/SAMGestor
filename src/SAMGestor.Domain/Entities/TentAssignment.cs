using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.Entities;

public class TentAssignment : Entity<Guid>
{
    public Guid TentId          { get; private set; }
    public Guid RegistrationId  { get; private set; } // Registration (FAZER)
    public int? Position        { get; private set; } // 0..Capacity-1
    public DateTimeOffset AssignedAt { get; private set; }
    public Guid? AssignedBy     { get; private set; }

    private TentAssignment() { }

    public TentAssignment(Guid tentId, Guid registrationId, int? position = null, Guid? assignedBy = null)
    {
        Id             = Guid.NewGuid();
        TentId         = tentId;
        RegistrationId = registrationId;
        Position       = position;
        AssignedBy     = assignedBy;
        AssignedAt     = DateTimeOffset.UtcNow;
    }

    public void MoveTo(Guid newTentId, int? newPosition = null, Guid? assignedBy = null)
    {
        TentId     = newTentId;
        Position   = newPosition;
        AssignedBy = assignedBy;
        AssignedAt = DateTimeOffset.UtcNow;
    }

    public void SetPosition(int? newPosition, Guid? assignedBy = null)
    {
        Position   = newPosition;
        AssignedBy = assignedBy;
        AssignedAt = DateTimeOffset.UtcNow;
    }
}