using SAMGestor.Domain.Entities;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Interfaces;

public interface IFamilyRepository
{
    Task AddAsync(Family family, CancellationToken ct = default);
    Task UpdateAsync(Family family, CancellationToken ct = default);
    Task<Family?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Family>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default);
    Task DeleteAllByRetreatAsync(Guid retreatId, CancellationToken ct = default);
    Task DeleteAsync(Family family, CancellationToken ct = default);
    Task<bool> ColorExistsInRetreatAsync(Guid retreatId, string colorName, Guid? excludeFamilyId = null, CancellationToken ct = default);
    Task<IReadOnlyList<string>> GetUsedColorsInRetreatAsync(Guid retreatId, CancellationToken ct = default);
}