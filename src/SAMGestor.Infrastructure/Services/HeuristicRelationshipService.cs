using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Infrastructure.Services;

public sealed class HeuristicRelationshipService : IRelationshipService
{
    private readonly IRegistrationRepository _registrations;
    public HeuristicRelationshipService(IRegistrationRepository registrations)
        => _registrations = registrations;

    public Task<bool> AreSpousesAsync(Guid id1, Guid id2, CancellationToken ct = default)
        => Task.FromResult(false);   // ainda não implementado

    public async Task<bool> AreDirectRelativesAsync(
        Guid id1, Guid id2, CancellationToken ct = default)
    {
        var r1 = await _registrations.GetByIdAsync(id1, ct);
        var r2 = await _registrations.GetByIdAsync(id2, ct);
        if (r1 is null || r2 is null) return false;

        var sameSurname = string.Equals(
            r1.Name.Last, r2.Name.Last, StringComparison.OrdinalIgnoreCase);

        if (!sameSurname) return false;

        var sameCity  = string.Equals(r1.City,  r2.City,  StringComparison.OrdinalIgnoreCase);
        var samePhone = string.Equals(r1.Phone, r2.Phone, StringComparison.OrdinalIgnoreCase);

        return sameCity || samePhone;
    }
}