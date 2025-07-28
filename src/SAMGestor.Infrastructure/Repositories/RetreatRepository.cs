using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Infrastructure.Persistence;


namespace SAMGestor.Infrastructure.Repositories;

public sealed class RetreatRepository : IRetreatRepository
{
    private readonly SAMContext _ctx;
    public RetreatRepository(SAMContext ctx) => _ctx = ctx;

    public async Task AddAsync(Retreat retreat, CancellationToken ct = default)
        => await _ctx.Retreats.AddAsync(retreat, ct);

    public Task<Retreat?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Retreats.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<bool> ExistsByNameEditionAsync(
        FullName name, string edition, CancellationToken ct = default)
    {
        return _ctx.Retreats.AnyAsync(r =>
            r.Name.Value == name.Value && r.Edition == edition.Trim(), ct);
    }
}