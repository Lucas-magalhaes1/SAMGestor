using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface IFamilyMemberRepository
{
    Task AddAsync(FamilyMember member, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<FamilyMember> members, CancellationToken ct = default);
    Task<IReadOnlyList<FamilyMember>> ListByRetreatAsync(Guid retreatId, CancellationToken ct = default);
    Task<IReadOnlyList<FamilyMember>> ListByFamilyAsync(Guid familyId, CancellationToken ct = default);
    Task<Dictionary<Guid, List<FamilyMember>>> ListByFamilyIdsAsync(IEnumerable<Guid> familyIds, CancellationToken ct = default);
    Task DeleteAllByRetreatAsync(Guid retreatId, CancellationToken ct = default);
    Task RemoveByFamilyIdAsync(Guid familyId, CancellationToken ct = default);
    Task RemoveRangeAsync(IEnumerable<FamilyMember> members, CancellationToken ct = default);
    Task UpdateAsync(FamilyMember member, CancellationToken ct = default);
    Task UpdateRangeAsync(IEnumerable<FamilyMember> members, CancellationToken ct = default);
    Task<FamilyMember?> GetByRegistrationIdAsync(Guid retreatId, Guid registrationId, CancellationToken ct = default);
}