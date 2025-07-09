using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Specifications;
using SAMGestor.Domain.ValueObjects;

public sealed class RegionQuotaSpecification : ISpecification<IEnumerable<Registration>>
{
    private readonly IReadOnlyDictionary<string, Percentage> _targets;

    public RegionQuotaSpecification(IEnumerable<RegionConfig> configs)
    {
        _targets = configs.ToDictionary(c => c.Name, c => c.TargetPercentage);
    }

    public bool IsSatisfiedBy(IEnumerable<Registration> registrations)
    {
        var total = registrations.Count();
        if (total == 0) return true;

        foreach (var grp in registrations.GroupBy(r => r.Region))
        {
            if (!_targets.TryGetValue(grp.Key, out var pct)) continue;

            var actual = grp.Count() * 100m / total;
            if (actual < (decimal)pct) return false;
        }
        return true;
    }
}