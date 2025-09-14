using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories;

public sealed class FamilyMemberRepository(SAMContext ctx) : IFamilyMemberRepository
{
    public async Task AddAsync(FamilyMember member, CancellationToken ct = default)
        => await ctx.FamilyMembers.AddAsync(member, ct);

    public async Task AddRangeAsync(IEnumerable<FamilyMember> members, CancellationToken ct = default)
        => await ctx.FamilyMembers.AddRangeAsync(members, ct);

    public async Task<IReadOnlyList<FamilyMember>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default)
        => await ctx.FamilyMembers
            .AsNoTracking()
            .Where(m => m.RetreatId == retreatId)
            .OrderBy(m => m.FamilyId).ThenBy(m => m.Position)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<FamilyMember>> ListByFamilyAsync(Guid familyId, CancellationToken ct = default)
        => await ctx.FamilyMembers
            .AsNoTracking()
            .Where(m => m.FamilyId == familyId)
            .OrderBy(m => m.Position)
            .ToListAsync(ct);

    public async Task<Dictionary<Guid, List<FamilyMember>>> ListByFamilyIdsAsync(IEnumerable<Guid> familyIds, CancellationToken ct = default)
    {
        var ids = familyIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<Guid, List<FamilyMember>>();

        var items = await ctx.FamilyMembers
            .AsNoTracking()
            .Where(m => ids.Contains(m.FamilyId))
            .OrderBy(m => m.FamilyId).ThenBy(m => m.Position)
            .ToListAsync(ct);

        return items
            .GroupBy(m => m.FamilyId)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task DeleteAllByRetreatAsync(Guid retreatId, CancellationToken ct = default)
    {
        var items = await ctx.FamilyMembers
            .Where(m => m.RetreatId == retreatId)
            .ToListAsync(ct);

        if (items.Count > 0)
            ctx.FamilyMembers.RemoveRange(items);
    }

    public async Task RemoveByFamilyIdAsync(Guid familyId, CancellationToken ct = default)
    {
        var items = await ctx.FamilyMembers
            .Where(m => m.FamilyId == familyId)
            .ToListAsync(ct);

        if (items.Count > 0)
            ctx.FamilyMembers.RemoveRange(items);
    }

    public Task RemoveRangeAsync(IEnumerable<FamilyMember> members, CancellationToken ct = default)
    {
        ctx.FamilyMembers.RemoveRange(members);
        return Task.CompletedTask;
    }
    
    public Task<FamilyMember?> GetByRegistrationIdAsync(Guid retreatId, Guid registrationId, CancellationToken ct = default)
        => ctx.FamilyMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.RetreatId == retreatId && m.RegistrationId == registrationId, ct);
}
