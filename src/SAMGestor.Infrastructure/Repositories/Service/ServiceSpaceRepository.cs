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
        => await db.ServiceSpaces.AsNoTracking()
            .Where(s => s.RetreatId == retreatId && s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ServiceSpace>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default)
        => await db.ServiceSpaces.AsNoTracking()
            .Where(s => s.RetreatId == retreatId)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public Task AddAsync(ServiceSpace space, CancellationToken ct = default)
        => db.ServiceSpaces.AddAsync(space, ct).AsTask();

    public Task AddRangeAsync(IEnumerable<ServiceSpace> spaces, CancellationToken ct = default)
    {
        db.ServiceSpaces.AddRange(spaces);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(ServiceSpace space, CancellationToken ct = default)
    {
        db.ServiceSpaces.Update(space);
        return Task.CompletedTask;
    }

    public async Task RemoveAsync(ServiceSpace space, CancellationToken ct = default)
    {
        db.ServiceSpaces.Remove(space);
        await Task.CompletedTask;
    }

    public Task<bool> HasActiveByRetreatAsync(Guid retreatId, CancellationToken ct = default)
        => db.ServiceSpaces.AsNoTracking().AnyAsync(s => s.RetreatId == retreatId && s.IsActive, ct);

    public Task<bool> ExistsByNameInRetreatAsync(Guid retreatId, string name, CancellationToken ct = default)
    {
        var norm = name.Trim();
        return db.ServiceSpaces.AsNoTracking()
            .AnyAsync(s => s.RetreatId == retreatId && s.Name == norm, ct);
    }
}
