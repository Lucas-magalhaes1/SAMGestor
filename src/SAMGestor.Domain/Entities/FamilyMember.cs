using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.Entities;

public class FamilyMember : Entity<Guid>
{
    public Guid RetreatId      { get; private set; }
    public Guid FamilyId       { get; private set; }
    public Guid RegistrationId { get; private set; }

    /// <summary>Ordem visual no card (drag-and-drop).</summary>
    public int Position        { get; private set; }

    public DateTime AssignedAt { get; private set; }

    private FamilyMember() { }

    public FamilyMember(Guid retreatId, Guid familyId, Guid registrationId, int position = 0)
    {
        Id             = Guid.NewGuid();
        RetreatId      = retreatId;
        FamilyId       = familyId;
        RegistrationId = registrationId;
        Position       = position;
        AssignedAt     = DateTime.UtcNow;
    }

    public void SetPosition(int position) => Position = position;
}