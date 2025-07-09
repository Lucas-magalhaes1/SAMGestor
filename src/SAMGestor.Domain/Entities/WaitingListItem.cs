using SAMGestor.Domain.Commom;
namespace SAMGestor.Domain.Entities;

public class WaitingListItem : Entity<Guid>
{
    public Guid RegistrationId { get; private set; }
    public Guid RetreatId      { get; private set; }
    public int  Position       { get; private set; }
    public DateTime CreatedAt  { get; private set; }

    private WaitingListItem() { }

    internal WaitingListItem(Guid registrationId, Guid retreatId, int position)
    {
        Id            = Guid.NewGuid();
        RegistrationId = registrationId;
        RetreatId      = retreatId;
        Position       = position;
        CreatedAt      = DateTime.UtcNow;
    }

    public void UpdatePosition(int newPosition) => Position = newPosition;
}