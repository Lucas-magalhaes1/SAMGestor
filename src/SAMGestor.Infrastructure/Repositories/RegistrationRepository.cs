using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories
{
    public sealed class RegistrationRepository : IRegistrationRepository
    {
        private readonly SAMContext _ctx;
        public RegistrationRepository(SAMContext ctx) => _ctx = ctx;

        public async Task AddAsync(Registration reg, CancellationToken ct = default)
            => await _ctx.Registrations.AddAsync(reg, ct);

        public Task<Registration?> GetByIdAsync(Guid id, CancellationToken ct = default)
            => _ctx.Registrations
                   .AsNoTracking()
                   .FirstOrDefaultAsync(r => r.Id == id, ct);

        public Task<bool> ExistsByCpfInRetreatAsync(CPF cpf, Guid retreatId, CancellationToken ct = default)
            => _ctx.Registrations
                   .AnyAsync(r => r.RetreatId == retreatId && r.Cpf.Value == cpf.Value, ct);

        public Task<bool> IsCpfBlockedAsync(CPF cpf, CancellationToken ct = default)
            => _ctx.BlockedCpfs
                   .AnyAsync(b => b.Cpf.Value == cpf.Value, ct);

        public async Task<IReadOnlyList<Registration>> ListAsync(
            Guid retreatId, string? status = null, string? region = null,
            int skip = 0, int take = 20, CancellationToken ct = default)
        {
            var query = _ctx.Registrations.Where(r => r.RetreatId == retreatId);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RegistrationStatus>(status, true, out var parsedStatus))
                query = query.Where(r => r.Status == parsedStatus);

            if (!string.IsNullOrEmpty(region))
                query = query.Where(r => r.Region == region);

            return await query.Skip(skip).Take(take).AsNoTracking().ToListAsync(ct);
        }

        public Task<int> CountAsync(Guid retreatId, string? status = null, string? region = null, CancellationToken ct = default)
        {
            var query = _ctx.Registrations.Where(r => r.RetreatId == retreatId);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RegistrationStatus>(status, true, out var parsedStatus))
                query = query.Where(r => r.Status == parsedStatus);

            if (!string.IsNullOrEmpty(region))
                query = query.Where(r => r.Region == region);

            return query.CountAsync(ct);
        }

        // ====== NOVOS ======

        public Task<int> CountByStatusesAndGenderAsync(Guid retreatId, RegistrationStatus[] statuses, Gender gender, CancellationToken ct)
            => _ctx.Registrations
                   .Where(r => r.RetreatId == retreatId
                            && statuses.Contains(r.Status)
                            && r.Gender == gender)
                   .CountAsync(ct);

        public Task<List<Guid>> ListAppliedIdsByGenderAsync(Guid retreatId, Gender gender, CancellationToken ct)
            => _ctx.Registrations
                   .Where(r => r.RetreatId == retreatId
                            && r.Enabled
                            && r.Status == RegistrationStatus.NotSelected
                            && r.Gender == gender)
                   .OrderBy(r => r.RegistrationDate) // opcional
                   .Select(r => r.Id)
                   .ToListAsync(ct);

        public async Task UpdateStatusesAsync(IEnumerable<Guid> registrationIds, RegistrationStatus newStatus, CancellationToken ct)
        {
            var ids = registrationIds.Distinct().ToList();
            if (ids.Count == 0) return;

            var regs = await _ctx.Registrations
                                 .Where(r => ids.Contains(r.Id))
                                 .ToListAsync(ct);

            foreach (var r in regs)
               r.SetStatus(newStatus);
        }
    }
}
