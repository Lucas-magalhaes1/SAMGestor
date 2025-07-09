using SAMGestor.Domain.Commom;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class RegionConfig : Entity<Guid>
{
    public string Name { get; private set; }
    public Percentage TargetPercentage { get; private set; }
    public string? Observation { get; private set; }
    public Guid RetreatId { get; private set; }

    private RegionConfig() { }

    public RegionConfig(string name, Percentage targetPercentage, Guid retreatId, string? observation = null)
    {
        Id = Guid.NewGuid();
        Name = name.Trim();
        TargetPercentage = targetPercentage;
        RetreatId = retreatId;
        Observation = observation?.Trim();
    }
}