using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories;

public sealed class ServiceAssignmentRepository(SAMContext db) : IServiceAssignmentRepository
{
    public async Task<IReadOnlyList<ServiceAssignment>> ListBySpaceIdsAsync(IEnumerable<Guid> spaceIds, CancellationToken ct = default)
    {
        var ids = spaceIds.Distinct().ToArray();
        if (ids.Length == 0) return Array.Empty<ServiceAssignment>();

        return await db.ServiceAssignments
            .AsNoTracking()
            .Where(a => ids.Contains(a.ServiceSpaceId))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ServiceAssignment>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default)
    {
        return await db.ServiceAssignments
            .AsNoTracking()
            .Join(db.ServiceSpaces.AsNoTracking(),
                  a => a.ServiceSpaceId,
                  s => s.Id,
                  (a, s) => new { a, s })
            .Where(x => x.s.RetreatId == retreatId)
            .Select(x => x.a)
            .ToListAsync(ct);
    }

    public Task<ServiceAssignment?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.ServiceAssignments.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

    public Task AddRangeAsync(IEnumerable<ServiceAssignment> links, CancellationToken ct = default)
    {
        db.ServiceAssignments.AddRange(links);
        return Task.CompletedTask;
    }

    public async Task RemoveByIdAsync(Guid assignmentId, CancellationToken ct = default)
    {
        var entity = await db.ServiceAssignments.FirstOrDefaultAsync(x => x.Id == assignmentId, ct);
        if (entity is null) return;
        db.ServiceAssignments.Remove(entity);
    }

    public async Task RemoveByRegistrationIdsAsync(Guid retreatId, IEnumerable<Guid> registrationIds, CancellationToken ct = default)
    {
        var ids = registrationIds.Distinct().ToArray();
        if (ids.Length == 0) return;

        var q = from a in db.ServiceAssignments
                join s in db.ServiceSpaces on a.ServiceSpaceId equals s.Id
                where s.RetreatId == retreatId && ids.Contains(a.ServiceRegistrationId)
                select a;

        var toRemove = await q.ToListAsync(ct);
        if (toRemove.Count > 0) db.ServiceAssignments.RemoveRange(toRemove);
    }

    public Task UpdateAsync(ServiceAssignment link, CancellationToken ct = default)
    {
        db.ServiceAssignments.Attach(link);
        db.Entry(link).State = EntityState.Modified;
        return Task.CompletedTask;
    }
    
    public async Task<(IReadOnlyList<ServiceAssignment> Items, int Total)> PageBySpaceAsync(
        Guid retreatId, Guid spaceId, int skip, int take, string? search, CancellationToken ct = default)
    {
        var baseQuery = from a in db.ServiceAssignments.AsNoTracking()
            join s in db.ServiceSpaces.AsNoTracking() on a.ServiceSpaceId equals s.Id
            where s.RetreatId == retreatId && s.Id == spaceId
            select new { a, s };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var q = search.Trim().ToLower();
            baseQuery =
                from pair in baseQuery
                join r in db.ServiceRegistrations.AsNoTracking() on pair.a.ServiceRegistrationId equals r.Id
                where r.Name.Value.ToLower().Contains(q)
                select new { a = pair.a, s = pair.s };
        }

        var total = await baseQuery.CountAsync(ct);

        var items = await baseQuery
            .OrderBy(x => x.a.Role) 
            .ThenBy(x => x.a.AssignedAt)
            .Skip(skip)
            .Take(take)
            .Select(x => x.a)
            .ToListAsync(ct);

        return (items, total);
    }
    
    public async Task RemoveBySpaceIdAsync(Guid spaceId, CancellationToken ct = default)
    {
        var toRemove = await db.ServiceAssignments
            .Where(a => a.ServiceSpaceId == spaceId)
            .ToListAsync(ct);

        if (toRemove.Count > 0)
            db.ServiceAssignments.RemoveRange(toRemove);
    }
}
