using SAMGestor.Domain.Entities;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Interfaces;

public interface IRetreatRepository
{
    Task AddAsync(Retreat retreat, CancellationToken ct = default);

    Task<Retreat?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<bool> ExistsByNameEditionAsync(
        FullName name,
        string   edition,
        CancellationToken ct = default);
}