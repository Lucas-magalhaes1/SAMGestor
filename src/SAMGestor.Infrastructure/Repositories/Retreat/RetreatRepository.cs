using Microsoft.EntityFrameworkCore;
using SAMGestor.Application.Common.Pagination;
using RetreatEntity = SAMGestor.Domain.Entities.Retreat; 
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories.Retreat;

public sealed class RetreatRepository : IRetreatRepository
{
    private readonly SAMContext _ctx;
    
    public RetreatRepository(SAMContext ctx) => _ctx = ctx;

    public async Task AddAsync(RetreatEntity retreat, CancellationToken ct = default)
        => await _ctx.Retreats.AddAsync(retreat, ct);

    public Task<RetreatEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Retreats.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<bool> ExistsByNameEditionAsync(
        FullName name, string edition, CancellationToken ct = default)
    {
        return _ctx.Retreats.AnyAsync(r =>
            r.Name.Value == name.Value && r.Edition == edition.Trim(), ct);
    }

    public async Task<(List<RetreatEntity> Items, int TotalCount)> ListAsync(
        int skip, 
        int take, 
        CancellationToken ct = default)
    {
        var query = _ctx.Retreats.AsNoTracking().OrderBy(r => r.StartDate);
    
        var totalCount = await query.CountAsync(ct);
    
        var retreats = await query
            .ApplyPagination(skip, take)
            .ToListAsync(ct);

        return (retreats, totalCount);
    }

    public Task<int> CountAsync(CancellationToken ct = default)
        => _ctx.Retreats.CountAsync(ct);

    public Task RemoveAsync(RetreatEntity retreat, CancellationToken ct = default)
    {
        _ctx.Retreats.Remove(retreat);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(RetreatEntity retreat, CancellationToken ct = default)
    {
        _ctx.Retreats.Update(retreat);
        return Task.CompletedTask;
    }
}