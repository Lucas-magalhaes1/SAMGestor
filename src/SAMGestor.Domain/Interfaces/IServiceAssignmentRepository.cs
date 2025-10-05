using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface IServiceAssignmentRepository
{
    Task<IReadOnlyList<ServiceAssignment>> ListBySpaceIdsAsync(IEnumerable<Guid> spaceIds, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceAssignment>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<ServiceAssignment> links, CancellationToken ct = default);
    Task UpdateAsync(ServiceAssignment link, CancellationToken ct = default);
    Task RemoveByIdAsync(Guid assignmentId, CancellationToken ct = default);
    Task RemoveBySpaceIdAsync(Guid spaceId, CancellationToken ct = default);
    Task RemoveByRegistrationIdsAsync(Guid retreatId, IEnumerable<Guid> registrationIds, CancellationToken ct = default);
    Task<(IReadOnlyList<ServiceAssignment> Items, int Total)> PageBySpaceAsync(
        Guid retreatId, Guid spaceId, int skip, int take, string? search, CancellationToken ct = default);
}