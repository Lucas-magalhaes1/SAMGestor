namespace SAMGestor.Domain.Interfaces;

public interface IRelationshipService
{
    Task<bool> AreSpousesAsync(Guid id1, Guid id2, CancellationToken ct = default);
    Task<bool> AreDirectRelativesAsync(Guid id1, Guid id2, CancellationToken ct = default);
}