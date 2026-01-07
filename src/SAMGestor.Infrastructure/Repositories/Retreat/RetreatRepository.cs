using Microsoft.EntityFrameworkCore;
using SAMGestor.Application.Common.Pagination;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Infrastructure.Persistence;


namespace SAMGestor.Infrastructure.Repositories;

public sealed class RetreatRepository(SAMContext ctx) : IRetreatRepository
{
    public async Task AddAsync(Retreat retreat, CancellationToken ct = default)
        => await ctx.Retreats.AddAsync(retreat, ct);

    public Task<Retreat?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => ctx.Retreats.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<bool> ExistsByNameEditionAsync(
        FullName name, string edition, CancellationToken ct = default)
    {
        return ctx.Retreats.AnyAsync(r =>
            r.Name.Value == name.Value && r.Edition == edition.Trim(), ct);
    }

    public async Task<(List<Retreat> Items, int TotalCount)> ListAsync(
        int skip, 
        int take, 
        CancellationToken ct = default)
    {
        var query = ctx.Retreats.AsNoTracking().OrderBy(r => r.StartDate);
    
        var totalCount = await query.CountAsync(ct);
    
        var retreats = await query
            .ApplyPagination(skip, take)
            .ToListAsync(ct);

        return (retreats, totalCount);
    }

    public Task<int> CountAsync(CancellationToken ct = default)
        => ctx.Retreats.CountAsync(ct);

    public Task RemoveAsync(Retreat retreat, CancellationToken ct = default)
    {
        ctx.Retreats.Remove(retreat);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Retreat retreat, CancellationToken ct = default)
    {
        ctx.Retreats.Update(retreat);
        return Task.CompletedTask;
    }
}