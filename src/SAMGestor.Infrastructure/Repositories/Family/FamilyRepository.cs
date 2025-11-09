using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories;

public sealed class FamilyRepository(SAMContext ctx) : IFamilyRepository
{
    public async Task AddAsync(Family family, CancellationToken ct = default)
        => await ctx.Families.AddAsync(family, ct);

    public Task UpdateAsync(Family family, CancellationToken ct = default)
    {
        ctx.Families.Update(family);
        return Task.CompletedTask;
    }

    public Task<Family?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => ctx.Families
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<Family>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default)
        => await ctx.Families
            .AsNoTracking()
            .Where(f => f.RetreatId == retreatId)
            .OrderBy(f => f.Id)
            .ToListAsync(ct);

    public async Task DeleteAllByRetreatAsync(Guid retreatId, CancellationToken ct = default)
    {
        var families = await ctx.Families
            .Where(f => f.RetreatId == retreatId)
            .ToListAsync(ct);

        if (families.Count > 0)
            ctx.Families.RemoveRange(families);
    }
    
    public Task DeleteAsync(Family family, CancellationToken ct = default)
    {
        ctx.Families.Remove(family);
        return Task.CompletedTask;
    }
}