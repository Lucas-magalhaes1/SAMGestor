using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories;

public sealed class TentRepository : ITentRepository
{
    private readonly SAMContext _db;
    public TentRepository(SAMContext db) => _db = db;

    public async Task<Tent?> GetByIdAsync(Guid tentId, CancellationToken ct = default)
        => await _db.Tents.FirstOrDefaultAsync(t => t.Id == tentId, ct);

    public async Task<List<Tent>> ListByRetreatAsync(Guid retreatId, TentCategory? category = null, bool? active = null, CancellationToken ct = default)
    {
        var q = _db.Tents.AsNoTracking().Where(t => t.RetreatId == retreatId);
        if (category.HasValue) q = q.Where(t => t.Category == category.Value);
        if (active.HasValue)   q = q.Where(t => t.IsActive == active.Value);
        return await q.OrderBy(t => t.Category).ThenBy(t => t.Number.Value).ToListAsync(ct);
    }

    public async Task<bool> ExistsNumberAsync(
        Guid retreatId,
        TentCategory category,
        TentNumber number,
        Guid? ignoreId = null,
        CancellationToken ct = default)
    {
        var num = number.Value; 

        var q = _db.Tents.AsNoTracking().Where(t =>
            t.RetreatId == retreatId &&
            t.Category  == category  &&
            t.Number.Value == num);

        if (ignoreId.HasValue)
            q = q.Where(t => t.Id != ignoreId.Value);

        return await q.AnyAsync(ct);
    }
   
    public async Task AddAsync(Tent tent, CancellationToken ct = default)
        => await _db.Tents.AddAsync(tent, ct);

    public Task UpdateAsync(Tent tent, CancellationToken ct = default)
    {
        _db.Tents.Update(tent);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Tent tent, CancellationToken ct = default)
    {
        _db.Tents.Remove(tent);
        return Task.CompletedTask;
    }
}