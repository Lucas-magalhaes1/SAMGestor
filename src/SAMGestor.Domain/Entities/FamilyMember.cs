using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.Entities;

public class FamilyMember : Entity<Guid>
{
    public Guid RetreatId      { get; private set; }
    public Guid FamilyId       { get; private set; }
    public Guid RegistrationId { get; private set; }
    public int Position        { get; private set; }
    public bool IsPadrinho { get; private set; }
    public bool IsMadrinha { get; private set; }

    public DateTime AssignedAt { get; private set; }

    private FamilyMember() { }

    public FamilyMember(
        Guid retreatId, 
        Guid familyId, 
        Guid registrationId, 
        int position = 0,
        bool isPadrinho = false,
        bool isMadrinha = false)
    {
        Id             = Guid.NewGuid();
        RetreatId      = retreatId;
        FamilyId       = familyId;
        RegistrationId = registrationId;
        Position       = position;
        IsPadrinho     = isPadrinho;
        IsMadrinha     = isMadrinha;
        AssignedAt     = DateTime.UtcNow;
    }

    public void SetPosition(int position) => Position = position;
    
    public void MarkAsPadrinho() => IsPadrinho = true;
    public void UnmarkAsPadrinho() => IsPadrinho = false;
    
    public void MarkAsMadrinha() => IsMadrinha = true;
    public void UnmarkAsMadrinha() => IsMadrinha = false;
}