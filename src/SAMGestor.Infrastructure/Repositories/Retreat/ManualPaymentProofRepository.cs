using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories.Retreat;

public class ManualPaymentProofRepository : IManualPaymentProofRepository
{
    private readonly SAMContext _ctx;
    public ManualPaymentProofRepository (SAMContext ctx) => _ctx = ctx;

    public async Task AddAsync(ManualPaymentProof proof, CancellationToken ct = default)
    {
        await _ctx.Set<ManualPaymentProof>().AddAsync(proof, ct);
    }

    public async Task<ManualPaymentProof?> GetByRegistrationIdAsync(Guid registrationId, CancellationToken ct = default)
    {
        return await _ctx.Set<ManualPaymentProof>()
            .FirstOrDefaultAsync(p => p.RegistrationId == registrationId, ct);
    }

    public async Task<ManualPaymentProof?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _ctx .Set<ManualPaymentProof>()
            .FirstOrDefaultAsync(p => p.Id == id, ct);
    }

    public async Task<bool> ExistsByRegistrationIdAsync(Guid registrationId, CancellationToken ct = default)
    {
        return await _ctx.Set<ManualPaymentProof>()
            .AnyAsync(p => p.RegistrationId == registrationId, ct);
    }
    public async Task<ManualPaymentProof?> GetByServiceRegistrationIdAsync(Guid serviceRegistrationId, CancellationToken ct = default)
    {
        return await _ctx.Set<ManualPaymentProof>()
            .FirstOrDefaultAsync(p => p.ServiceRegistrationId == serviceRegistrationId, ct);
    }

    public async Task<bool> ExistsByServiceRegistrationIdAsync(Guid serviceRegistrationId, CancellationToken ct = default)
    {
        return await _ctx.Set<ManualPaymentProof>()
            .AnyAsync(p => p.ServiceRegistrationId == serviceRegistrationId, ct);
    }
}