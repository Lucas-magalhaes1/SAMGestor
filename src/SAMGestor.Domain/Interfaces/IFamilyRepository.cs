using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface IFamilyRepository
{
    Task AddAsync(Family family, CancellationToken ct = default);
    Task UpdateAsync(Family family, CancellationToken ct = default);
    Task<Family?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Family>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default);
    Task DeleteAllByRetreatAsync(Guid retreatId, CancellationToken ct = default);
}