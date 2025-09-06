using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Interfaces;

public interface IRegistrationRepository
{
    Task AddAsync(Registration reg, CancellationToken ct = default);
    Task<bool> ExistsByCpfInRetreatAsync(CPF cpf, Guid retreatId, CancellationToken ct = default);
    Task<bool> IsCpfBlockedAsync(CPF cpf, CancellationToken ct = default);
    Task<Registration?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Registration>> ListAsync(
        Guid retreatId,
        string? status = null,
        string? region = null,
        int skip = 0,
        int take = 20,
        CancellationToken ct = default);
    Task<int> CountAsync(
        Guid retreatId,
        string? status = null,
        string? region = null,
        CancellationToken ct = default);
    
    Task<int> CountByStatusesAndGenderAsync(
        Guid retreatId,
        RegistrationStatus[] statuses,
        Gender gender,
        CancellationToken ct);

    Task<List<Guid>> ListAppliedIdsByGenderAsync(
        Guid retreatId,
        Gender gender,
        CancellationToken ct);

    Task UpdateStatusesAsync(
        IEnumerable<Guid> registrationIds,
        RegistrationStatus newStatus,
        CancellationToken ct);
    
    Task<Dictionary<Guid, Registration>> GetMapByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
}