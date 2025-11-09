using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories;

public sealed class TentAssignmentRepository : ITentAssignmentRepository
{
    private readonly SAMContext _db;
    public TentAssignmentRepository(SAMContext db) => _db = db;

    public async Task<TentAssignment?> GetByRegistrationIdAsync(Guid registrationId, CancellationToken ct = default)
        => await _db.TentAssignments.FirstOrDefaultAsync(a => a.RegistrationId == registrationId, ct);

    public async Task<List<TentAssignment>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default)
        => await _db.TentAssignments
            .Where(a => _db.Tents.Any(t => t.Id == a.TentId && t.RetreatId == retreatId))
            .ToListAsync(ct);

    public async Task<List<TentAssignment>> ListByTentIdsAsync(Guid[] tentIds, CancellationToken ct = default)
        => await _db.TentAssignments.Where(a => tentIds.Contains(a.TentId)).ToListAsync(ct);

    public async Task<int> CountByTentIdAsync(Guid tentId, CancellationToken ct = default)
        => await _db.TentAssignments.CountAsync(a => a.TentId == tentId, ct);

    public async Task<Dictionary<Guid,int>> CountByTentIdsAsync(Guid[] tentIds, CancellationToken ct = default)
        => await _db.TentAssignments
            .Where(a => tentIds.Contains(a.TentId))
            .GroupBy(a => a.TentId)
            .Select(g => new { g.Key, Cnt = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Cnt, ct);

    public async Task<bool> AnyForRegistrationAsync(Guid registrationId, CancellationToken ct = default)
        => await _db.TentAssignments.AnyAsync(a => a.RegistrationId == registrationId, ct);

    public async Task AddAsync(TentAssignment assignment, CancellationToken ct = default)
        => await _db.TentAssignments.AddAsync(assignment, ct);

    public async Task AddRangeAsync(IEnumerable<TentAssignment> assignments, CancellationToken ct = default)
        => await _db.TentAssignments.AddRangeAsync(assignments, ct);

    public async Task RemoveByRegistrationIdAsync(Guid registrationId, CancellationToken ct = default)
    {
        var item = await _db.TentAssignments.FirstOrDefaultAsync(a => a.RegistrationId == registrationId, ct);
        if (item is not null) _db.TentAssignments.Remove(item);
    }

    public Task RemoveRangeAsync(IEnumerable<TentAssignment> assignments, CancellationToken ct = default)
    {
        _db.TentAssignments.RemoveRange(assignments);
        return Task.CompletedTask;
    }
}
