using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories;

public sealed class ServiceSpaceRepository(SAMContext db) : IServiceSpaceRepository
{
    public Task<ServiceSpace?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.ServiceSpaces.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<ServiceSpace>> ListActiveByRetreatAsync(Guid retreatId, CancellationToken ct = default)
    {
        var list = await db.ServiceSpaces
            .AsNoTracking()
            .Where(s => s.RetreatId == retreatId && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return list;
    }
    
    public async Task<IReadOnlyList<ServiceSpace>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default)
    {
        var list = await db.ServiceSpaces
            .AsNoTracking()
            .Where(s => s.RetreatId == retreatId)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return list;
    }
    
    public Task AddAsync(ServiceSpace space, CancellationToken ct = default)
        => db.ServiceSpaces.AddAsync(space, ct).AsTask();
    
    public Task AddRangeAsync(IEnumerable<ServiceSpace> spaces, CancellationToken ct = default)
    {
        db.ServiceSpaces.AddRange(spaces);
        return Task.CompletedTask;
    }

    public Task<bool> HasActiveByRetreatAsync(Guid retreatId, CancellationToken ct = default)
        => db.ServiceSpaces.AsNoTracking().AnyAsync(s => s.RetreatId == retreatId && s.IsActive, ct);
}