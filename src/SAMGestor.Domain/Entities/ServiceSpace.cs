using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.Entities;

public class ServiceSpace : Entity<Guid>
{
    public Guid   RetreatId   { get; private set; }
    public string Name        { get; private set; } = default!;
    public string? Description { get; private set; }
    public int    MinPeople   { get; private set; }
    public int    MaxPeople   { get; private set; }
    public bool   IsLocked    { get; private set; }
    public bool   IsActive    { get; private set; }

    private ServiceSpace() { }

    public ServiceSpace(Guid retreatId, string name, string? description, int maxPeople, int minPeople = 0)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(name));
        if (maxPeople <= 0) throw new ArgumentException(nameof(maxPeople));
        if (minPeople < 0 || minPeople > maxPeople) throw new ArgumentException(nameof(minPeople));

        Id          = Guid.NewGuid();
        RetreatId   = retreatId;
        Name        = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        MinPeople   = minPeople;
        MaxPeople   = maxPeople;
        IsLocked    = false;
        IsActive    = true;
    }
    
    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(name));
        Name = name.Trim();
    }
    
    public void UpdateDescription(string? description)
        => Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

    public void UpdateBasics(string name, string? description)
    {
        if (IsLocked) throw new InvalidOperationException("Space is locked.");
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException(nameof(name));
        Name        = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
    }

    public void UpdateCapacity(int minPeople, int maxPeople)
    {
        if (IsLocked) throw new InvalidOperationException("Space is locked.");
        if (maxPeople <= 0) throw new ArgumentException(nameof(maxPeople));
        if (minPeople < 0 || minPeople > maxPeople) throw new ArgumentException(nameof(minPeople));
        MinPeople = minPeople;
        MaxPeople = maxPeople;
    }

    public void Lock()   => IsLocked = true;
    public void Unlock() => IsLocked = false;

    public void Activate()   => IsActive = true;
    public void Deactivate() => IsActive = false;
}
