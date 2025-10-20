using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class Tent : Entity<Guid>
{
    public TentNumber   Number    { get; private set; }
    public TentCategory Category  { get; private set; } // Male/Female
    public int          Capacity  { get; private set; } // >= 1
    public Guid         RetreatId { get; private set; }

    public bool   IsActive { get; private set; } = true;
    public bool   IsLocked { get; private set; } = false;
    public string? Notes   { get; private set; }

    private Tent() { }

    public Tent(TentNumber number, TentCategory category, int capacity, Guid retreatId, string? notes = null)
    {
        if (!Enum.IsDefined(typeof(TentCategory), category))
            throw new ArgumentException(nameof(category));
        if (capacity <= 0)
            throw new ArgumentException(nameof(capacity));

        Id        = Guid.NewGuid();
        Number    = number;
        Category  = category;
        Capacity  = capacity;
        RetreatId = retreatId;
        Notes     = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }

    public void UpdateNumber(TentNumber number) => Number = number;

    public void UpdateCategory(TentCategory category)
    {
        if (!Enum.IsDefined(typeof(TentCategory), category))
            throw new ArgumentException(nameof(category));
        Category = category;
    }

    public void UpdateCapacity(int capacity)
    {
        if (capacity <= 0) throw new ArgumentException(nameof(capacity));
        Capacity = capacity;
    }

    public void UpdateNotes(string? notes) => Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();

    public void Activate()   => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void Lock()   => IsLocked = true;
    public void Unlock() => IsLocked = false;
}