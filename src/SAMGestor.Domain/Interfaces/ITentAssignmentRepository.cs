using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface ITentAssignmentRepository
{
    Task<TentAssignment?> GetByRegistrationIdAsync(Guid registrationId, CancellationToken ct = default);
    Task<List<TentAssignment>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default);
    Task<List<TentAssignment>> ListByTentIdsAsync(Guid[] tentIds, CancellationToken ct = default);
    Task<int> CountByTentIdAsync(Guid tentId, CancellationToken ct = default);
    Task<Dictionary<Guid,int>> CountByTentIdsAsync(Guid[] tentIds, CancellationToken ct = default);

    Task<bool> AnyForRegistrationAsync(Guid registrationId, CancellationToken ct = default);

    Task AddAsync(TentAssignment assignment, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<TentAssignment> assignments, CancellationToken ct = default);
    Task RemoveByRegistrationIdAsync(Guid registrationId, CancellationToken ct = default);
    Task RemoveRangeAsync(IEnumerable<TentAssignment> assignments, CancellationToken ct = default);
}