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
        
        public Task<Registration?> GetByIdForUpdateAsync(Guid id, CancellationToken ct = default)
            => _ctx.Registrations
                .FirstOrDefaultAsync(r => r.Id == id, ct);

        public Task<bool> ExistsByCpfInRetreatAsync(CPF cpf, Guid retreatId, CancellationToken ct = default)
            => _ctx.Registrations.AsNoTracking()
                .AnyAsync(r => r.RetreatId == retreatId && r.Cpf == cpf, ct);

        public Task<bool> IsCpfBlockedAsync(CPF cpf, CancellationToken ct = default)
            => _ctx.BlockedCpfs.AsNoTracking()
                .AnyAsync(b => b.Cpf == cpf, ct);
        
        public async Task<IReadOnlyList<Registration>> ListAsync(
            Guid retreatId, string? status = null, string? region = null,
            int skip = 0, int take = 20, CancellationToken ct = default)
        {
            var query = _ctx.Registrations.Where(r => r.RetreatId == retreatId);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RegistrationStatus>(status, true, out var parsedStatus))
                query = query.Where(r => r.Status == parsedStatus);
            return await query.Skip(skip).Take(take).AsNoTracking().ToListAsync(ct);
        }

        public Task<int> CountAsync(Guid retreatId, string? status = null, string? region = null, CancellationToken ct = default)
        {
            var query = _ctx.Registrations.Where(r => r.RetreatId == retreatId);

            if (!string.IsNullOrEmpty(status) && Enum.TryParse<RegistrationStatus>(status, true, out var parsedStatus))
                query = query.Where(r => r.Status == parsedStatus);
            return query.CountAsync(ct);
        }
        
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
        
        public async Task<Dictionary<Guid, Registration>> GetMapByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        {
            var idArray = ids?.Distinct().ToArray() ?? Array.Empty<Guid>();
            if (idArray.Length == 0)
                return new Dictionary<Guid, Registration>();

            var list = await _ctx.Registrations
                .Where(r => idArray.Contains(r.Id))
                .AsNoTracking()
                .ToListAsync(ct);

            return list.ToDictionary(r => r.Id, r => r);
        }
        
        public async Task<List<Registration>> ListPaidByRetreatAndGenderAsync(Guid retreatId, Gender gender, CancellationToken ct = default)
            => await _ctx.Registrations.AsNoTracking()
                .Where(r => r.RetreatId == retreatId &&
                            r.Enabled &&
                            (r.Status == RegistrationStatus.PaymentConfirmed || r.Status == RegistrationStatus.Confirmed) &&
                            r.Gender == gender)
                .OrderBy(r => r.Name.Value)
                .ToListAsync(ct);

        public async Task<List<Registration>> ListPaidByRetreatAsync(Guid retreatId, CancellationToken ct = default)
            => await _ctx.Registrations.AsNoTracking()
                .Where(r => r.RetreatId == retreatId &&
                            r.Enabled &&
                            (r.Status == RegistrationStatus.PaymentConfirmed || r.Status == RegistrationStatus.Confirmed))
                .OrderBy(r => r.Gender).ThenBy(r => r.Name.Value)
                .ToListAsync(ct);

        public async Task<List<Registration>> ListPaidUnassignedAsync(Guid retreatId, Gender? gender = null,
            string? search = null, CancellationToken ct = default)
        {
            var q = _ctx.Registrations.AsNoTracking()
                .Where(r => r.RetreatId == retreatId &&
                            r.Enabled &&
                            (r.Status == RegistrationStatus.PaymentConfirmed ||
                             r.Status == RegistrationStatus.Confirmed));

            if (gender.HasValue) q = q.Where(r => r.Gender == gender.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                q = q.Where(r =>
                    r.Name.Value.ToLower().Contains(s) ||
                    r.Email.Value.ToLower().Contains(s) ||
                    r.Cpf.Value.ToLower().Contains(s));
            }

            // Sem barraca: não existe assignment
            q = q.Where(r => !_ctx.TentAssignments.Any(a => a.RegistrationId == r.Id));

            return await q.OrderBy(r => r.Name.Value).ToListAsync(ct);
        }
        
        public async Task<int> CountByTentAsync(Guid tentId, CancellationToken ct = default)
        {
            return await _ctx.Registrations
                .AsNoTracking()
                .CountAsync(r => r.TentId == tentId, ct);
        }

        public async Task<Dictionary<Guid,int>> GetAssignedCountMapByTentIdsAsync(
            Guid retreatId,
            Guid[] tentIds,
            CancellationToken ct = default)
        {
            if (tentIds.Length == 0) return new Dictionary<Guid,int>();

            var rows = await _ctx.Registrations
                .AsNoTracking()
                .Where(r => r.RetreatId == retreatId &&
                            r.TentId != null &&
                            tentIds.Contains(r.TentId.Value))
                .GroupBy(r => r.TentId!.Value)
                .Select(g => new { TentId = g.Key, Count = g.Count() })
                .ToListAsync(ct);

            return rows.ToDictionary(x => x.TentId, x => x.Count);
        }
    }
}
