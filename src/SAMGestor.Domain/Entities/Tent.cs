using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class Tent : Entity<Guid>
{
    public TentNumber Number { get; private set; }
    public TentCategory Category { get; private set; }
    public int Capacity { get; private set; }
    public Guid RetreatId { get; private set; }

    private Tent() { }

    public Tent(TentNumber number, TentCategory category, int capacity, Guid retreatId)
    {
        Id = Guid.NewGuid();
        Number = number;
        Category = category;
        Capacity = capacity;
        RetreatId = retreatId;
    }
}