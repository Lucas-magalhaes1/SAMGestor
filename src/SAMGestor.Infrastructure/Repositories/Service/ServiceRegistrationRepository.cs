using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories;

public sealed class ServiceRegistrationRepository(SAMContext db) : IServiceRegistrationRepository
{
    public Task AddAsync(ServiceRegistration entity, CancellationToken ct = default)
        => db.ServiceRegistrations.AddAsync(entity, ct).AsTask();

    public Task<bool> ExistsByCpfInRetreatAsync(CPF cpf, Guid retreatId, CancellationToken ct = default)
    {
        var value = cpf.Value;
        return db.ServiceRegistrations
            .AsNoTracking()
            .AnyAsync(x => x.RetreatId == retreatId && x.Cpf == value, ct);
    }

    public Task<bool> ExistsByEmailInRetreatAsync(EmailAddress email, Guid retreatId, CancellationToken ct = default)
    {
        var value = email.Value;
        return db.ServiceRegistrations
            .AsNoTracking()
            .AnyAsync(x => x.RetreatId == retreatId && x.Email == value, ct);
    }

    public Task<bool> IsCpfBlockedAsync(CPF cpf, CancellationToken ct = default)
    {
        var value = cpf.Value;
        return db.BlockedCpfs
            .AsNoTracking()
            .AnyAsync(x => x.Cpf == value, ct);
    }
    
    public async Task<IDictionary<Guid, int>> CountPreferencesBySpaceAsync(Guid retreatId, CancellationToken ct = default)
    {
        var data = await db.ServiceRegistrations
            .AsNoTracking()
            .Where(r => r.RetreatId == retreatId && r.PreferredSpaceId != null)
            .GroupBy(r => r.PreferredSpaceId!.Value)
            .Select(g => new { SpaceId = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        return data.ToDictionary(x => x.SpaceId, x => x.Count);
    }
    
    public async Task<Dictionary<Guid, ServiceRegistration>> GetMapByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var set = ids.Distinct().ToArray();
        if (set.Length == 0) return new();

        var list = await db.ServiceRegistrations.AsNoTracking()
            .Where(r => set.Contains(r.Id))
            .ToListAsync(ct);

        return list.ToDictionary(r => r.Id);
    }
    
    public async Task<IReadOnlyList<ServiceRegistration>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default)
    {
        var list = await db.ServiceRegistrations.AsNoTracking()
            .Where(r => r.RetreatId == retreatId)
            .ToListAsync(ct);

        return list;
    }
    public Task<ServiceRegistration?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => db.ServiceRegistrations.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);

    public async Task ClearPreferenceBySpaceIdAsync(Guid spaceId, CancellationToken ct = default)
    {
        await db.ServiceRegistrations
            .Where(r => r.PreferredSpaceId == spaceId)
            .ExecuteUpdateAsync(setters =>
                    setters.SetProperty(r => r.PreferredSpaceId, r => (Guid?)null),
                ct);
    }
    public Task<ServiceRegistration?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default)
        => db.ServiceRegistrations
            .FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task UpdateAsync(ServiceRegistration serviceRegistration, CancellationToken ct = default)
    {
        db.ServiceRegistrations.Update(serviceRegistration);
        return Task.CompletedTask;
    }
}