using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Lottery;

public sealed class LotteryPreviewHandler(
    IRetreatRepository retreatRepo,
    IRegistrationRepository regRepo)
    : IRequestHandler<LotteryPreviewQuery, LotteryResultDto>
{
    public async Task<LotteryResultDto> Handle(LotteryPreviewQuery q, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(q.RetreatId, ct)
                      ?? throw new NotFoundException(nameof(Retreat), q.RetreatId);

        if (retreat.ContemplationClosed)
            throw new BusinessRuleException("Contemplation is closed for this retreat.");

        // Capacidade atual por gênero
        var maleOcc = await regRepo.CountByStatusesAndGenderAsync(q.RetreatId, SlotPolicy.OccupyingStatuses, Gender.Male, ct);
        var femOcc  = await regRepo.CountByStatusesAndGenderAsync(q.RetreatId, SlotPolicy.OccupyingStatuses, Gender.Female, ct);

        var maleCap = Math.Max(0, retreat.MaleSlots   - maleOcc);
        var femCap  = Math.Max(0, retreat.FemaleSlots - femOcc);

        // Buscar registrations completas (precisa de City e BirthDate pra prioridade)
        var allApplied = await regRepo.ListAppliedByGenderAsync(q.RetreatId, ct);

        var malePool   = allApplied.Where(r => r.Gender == Gender.Male).ToList();
        var femalePool = allApplied.Where(r => r.Gender == Gender.Female).ToList();

        // Processar homens
        var (malePick, malePriority) = ProcessGenderPool(malePool, maleCap, retreat, q.PriorityCities, q.MinAge, q.MaxAge);

        // Processar mulheres
        var (femPick, femPriority) = ProcessGenderPool(femalePool, femCap, retreat, q.PriorityCities, q.MinAge, q.MaxAge);

        return new LotteryResultDto(
            Male: malePick,
            Female: femPick,
            MaleCapacity: maleCap,
            FemaleCapacity: femCap,
            MalePriority: malePriority,
            FemalePriority: femPriority
        );
    }

    private static (List<Guid> selected, List<Guid> priority) ProcessGenderPool(
        List<Registration> pool,
        int capacity,
        Retreat retreat,
        List<string>? priorityCities,
        int? minAge,
        int? maxAge)
    {
        if (capacity == 0 || pool.Count == 0)
            return (new List<Guid>(), new List<Guid>());

        var retreatDate = retreat.StartDate;
        var hasPriorityCriteria = (priorityCities?.Count > 0) || (minAge.HasValue || maxAge.HasValue);
        
        if (!hasPriorityCriteria)
        {
            var ids = pool.Select(r => r.Id).ToList();
            Shuffler.ShuffleInPlace(ids);
            var picked = ids.Take(capacity).ToList(); 
            return (picked, new List<Guid>());
        }

        // Normalizar cidades pra comparação (lowercase, trim)
        var normalizedCities = priorityCities?
            .Select(c => c.Trim().ToLowerInvariant())
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .ToHashSet() ?? new HashSet<string>();

        // Separar prioritários vs não-prioritários
        var priorityList = new List<Registration>();
        var regularList = new List<Registration>();

        foreach (var reg in pool)
        {
            var isPriority = false;

            // Checa cidade
            if (normalizedCities.Count > 0)
            {
                var regCity = (reg.City ?? string.Empty).Trim().ToLowerInvariant();
                if (normalizedCities.Contains(regCity))
                    isPriority = true;
            }

            // Checa idade
            if (minAge.HasValue || maxAge.HasValue)
            {
                var age = reg.GetAgeOn(retreatDate);
                var meetsMinAge = !minAge.HasValue || age >= minAge.Value;
                var meetsMaxAge = !maxAge.HasValue || age <= maxAge.Value;

                if (meetsMinAge && meetsMaxAge)
                    isPriority = true;
            }

            if (isPriority)
                priorityList.Add(reg);
            else
                regularList.Add(reg);
        }

        // Embaralhar cada grupo
        Shuffler.ShuffleInPlace(priorityList);
        Shuffler.ShuffleInPlace(regularList);

        // Montar resultado: prioritários primeiro, depois regulares
        var selected = new List<Guid>();
        var priorityIds = new List<Guid>();

        // Preenche com prioritários até acabar capacidade ou lista
        foreach (var reg in priorityList)
        {
            if (selected.Count >= capacity) break;
            selected.Add(reg.Id);
            priorityIds.Add(reg.Id);
        }

        // Preenche resto com regulares (se ainda tiver vaga)
        foreach (var reg in regularList)
        {
            if (selected.Count >= capacity) break;
            selected.Add(reg.Id);
        }

        return (selected, priorityIds);
    }
}
