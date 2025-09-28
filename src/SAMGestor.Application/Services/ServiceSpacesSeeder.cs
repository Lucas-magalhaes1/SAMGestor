// src/SAMGestor.Application/Services/ServiceSpacesSeeder.cs
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Services;

public static class DefaultServiceSpaces
{
    public static readonly string[] Names =
    [
        "Casa da Mãe (CDM)",
        "Casa do Pai (CDP)",
        "Tapera",
        "Apoio",
        "Manutenção",
        "Cozinha",
        "Externa",
        "Loja",
        "Capela",
        "Guardião",
        "Cantina",
        "Madrinha",
        "Padrinho",
        "Música",
        "Teatro",
        "Saúde",
        "Secretaria"
    ];
}

public sealed class ServiceSpacesSeeder(IServiceSpaceRepository repo)
{
    public async Task SeedDefaultsIfMissingAsync(
        Guid retreatId,
        int defaultMaxPeople,
        CancellationToken ct = default)
    {
        var existing = await repo.ListByRetreatAsync(retreatId, ct);
        var existingNames = existing.Select(s => s.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var toCreate = new List<ServiceSpace>();
        foreach (var name in DefaultServiceSpaces.Names)
        {
            if (!existingNames.Contains(name))
            {
                // descrição nula; gestor poderá editar depois
                toCreate.Add(new ServiceSpace(retreatId, name, description: null, maxPeople: defaultMaxPeople, minPeople: 0));
            }
        }

        if (toCreate.Count > 0)
            await repo.AddRangeAsync(toCreate, ct);
    }
}