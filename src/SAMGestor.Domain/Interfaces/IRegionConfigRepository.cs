using SAMGestor.Domain.Entities;

public interface IRegionConfigRepository
{
    IEnumerable<RegionConfig> GetByRetreat(Guid retreatId);
}