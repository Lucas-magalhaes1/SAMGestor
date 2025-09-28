using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface IServiceSpaceRepository
{
    Task<ServiceSpace?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceSpace>> ListActiveByRetreatAsync(Guid retreatId, CancellationToken ct = default);
    Task<IReadOnlyList<ServiceSpace>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default);
    Task<bool> HasActiveByRetreatAsync(Guid retreatId, CancellationToken ct = default);
    
    Task AddAsync(ServiceSpace space, CancellationToken ct = default); 
    
    Task AddRangeAsync(IEnumerable<ServiceSpace> spaces, CancellationToken ct = default); 
}