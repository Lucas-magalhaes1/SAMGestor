using SAMGestor.Application.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.UnitOfWork;

public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly SAMContext _ctx;
    public EfUnitOfWork(SAMContext ctx) => _ctx = ctx;

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _ctx.SaveChangesAsync(ct);
}