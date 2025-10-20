using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Interfaces;

public  interface ITentRepository
{
    Task<Tent?> GetByIdAsync(Guid tentId, CancellationToken ct = default);
    Task<List<Tent>> ListByRetreatAsync(Guid retreatId, TentCategory? category = null, bool? active = null, CancellationToken ct = default);
    Task<bool> ExistsNumberAsync(Guid retreatId, TentCategory category, TentNumber number, Guid? ignoreId = null, CancellationToken ct = default);
    Task AddAsync(Tent tent, CancellationToken ct = default);
    Task UpdateAsync(Tent tent, CancellationToken ct = default);
    Task DeleteAsync(Tent tent, CancellationToken ct = default);
}