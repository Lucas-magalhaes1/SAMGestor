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
        return db.ServiceRegistrations
            .AsNoTracking()
            .AnyAsync(x => x.RetreatId == retreatId && x.Cpf.Value == cpf.Value, ct);
    }

    public Task<bool> ExistsByEmailInRetreatAsync(EmailAddress email, Guid retreatId, CancellationToken ct = default)
    {
        return db.ServiceRegistrations
            .AsNoTracking()
            .AnyAsync(x => x.RetreatId == retreatId && x.Email.Value == email.Value, ct);
    }

    public Task<bool> IsCpfBlockedAsync(CPF cpf, CancellationToken ct = default)
    {
        return db.BlockedCpfs
            .AsNoTracking()
            .AnyAsync(x => x.Cpf.Value == cpf.Value, ct);
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
}