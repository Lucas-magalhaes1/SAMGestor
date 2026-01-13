using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories.Family;

public sealed class FamilyRepository(SAMContext ctx) : IFamilyRepository
{
    public async Task AddAsync(Domain.Entities.Family family, CancellationToken ct = default)
        => await ctx.Families.AddAsync(family, ct);

    public Task UpdateAsync(Domain.Entities.Family family, CancellationToken ct = default)
    {
        ctx.Families.Update(family);
        return Task.CompletedTask;
    }

    public Task<Domain.Entities.Family?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => ctx.Families
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<IReadOnlyList<Domain.Entities.Family>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default)
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
    
    public Task DeleteAsync(Domain.Entities.Family family, CancellationToken ct = default)
    {
        ctx.Families.Remove(family);
        return Task.CompletedTask;
    }
    
    public async Task<bool> ColorExistsInRetreatAsync(
        Guid retreatId, 
        string colorName, 
        Guid? excludeFamilyId = null, 
        CancellationToken ct = default)
    {
        var normalizedColor = colorName.Trim().ToLowerInvariant();
        
        var query = ctx.Families
            .AsNoTracking()
            .Where(f => f.RetreatId == retreatId);

        if (excludeFamilyId.HasValue)
            query = query.Where(f => f.Id != excludeFamilyId.Value);

        return await query.AnyAsync(
            f => EF.Property<string>(f.Color, "Name").ToLower() == normalizedColor, 
            ct);
    }
    
    public async Task<IReadOnlyList<string>> GetUsedColorsInRetreatAsync(
        Guid retreatId, 
        CancellationToken ct = default)
    {
        return await ctx.Families
            .AsNoTracking()
            .Where(f => f.RetreatId == retreatId)
            .Select(f => EF.Property<string>(f.Color, "Name"))
            .Distinct()
            .ToListAsync(ct);
    }
}
