using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Infrastructure.Services;

public sealed class HeuristicRelationshipService : IRelationshipService
{
    private readonly IRegistrationRepository _registrations;

    public HeuristicRelationshipService(IRegistrationRepository registrations)
    {
        _registrations = registrations;
    }

    // Ainda não há campo explícito de cônjuge → false sempre
    public bool AreSpouses(Guid id1, Guid id2) => false;

    public bool AreDirectRelatives(Guid id1, Guid id2)
    {
        var r1 = _registrations.GetById(id1);
        var r2 = _registrations.GetById(id2);
        if (r1 is null || r2 is null) return false;

        // Nível 1 – mesmo sobrenome
        var sameSurname = string.Equals(
            r1.Name.Last, r2.Name.Last, StringComparison.OrdinalIgnoreCase);

        if (!sameSurname) return false;

        // Nível 2 – cidade OU telefone iguais
        var sameCity  = string.Equals(r1.City,  r2.City,  StringComparison.OrdinalIgnoreCase);
        var samePhone = string.Equals(r1.Phone, r2.Phone, StringComparison.OrdinalIgnoreCase);

        return sameCity || samePhone;
    }
}