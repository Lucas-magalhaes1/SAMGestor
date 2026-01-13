using MediatR;
using SAMGestor.Application.Common.Families;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Application.Features.Families.Generate;

public sealed class GenerateFamiliesHandler(
    IRetreatRepository retreatRepo,
    IRegistrationRepository registrationRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository familyMemberRepo,
    IUnitOfWork uow
) : IRequestHandler<GenerateFamiliesCommand, GenerateFamiliesResponse>
{
    private record PersonInfo(
        Guid Id,
        string NormalizedSurname,
        string City,
        Gender Gender,
        string? FatherName,
        string? MotherName,
        string? SubmitterNames,
        bool HasRelatives,
        Registration Registration
    );

    public async Task<GenerateFamiliesResponse> Handle(GenerateFamiliesCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct)
            ?? throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        if (retreat.FamiliesLocked)
            throw new BusinessRuleException("Famílias estão bloqueadas para edição.");

        var existingFamilies = await familyRepo.ListByRetreatAsync(cmd.RetreatId, ct);
        var anyLocked = existingFamilies.Any(f => f.IsLocked);

        if (cmd.ReplaceExisting && anyLocked)
            throw new BusinessRuleException("Existem famílias bloqueadas. Desbloqueie antes de gerar novamente.");

        if (cmd.ReplaceExisting)
        {
            await familyMemberRepo.DeleteAllByRetreatAsync(cmd.RetreatId, ct);
            await familyRepo.DeleteAllByRetreatAsync(cmd.RetreatId, ct);
            existingFamilies = new List<Family>();
        }

        var pool = await GetEligibleParticipantsAsync(cmd.RetreatId, cmd.ReplaceExisting, ct);

        if (pool.Count == 0)
            return new GenerateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<GeneratedFamilyDto>());

        var males = pool
            .Where(p => p.Gender == Gender.Male)
            .Select(p => CreatePersonInfo(p))
            .ToList();

        var females = pool
            .Where(p => p.Gender == Gender.Female)
            .Select(p => CreatePersonInfo(p))
            .ToList();

        var allPeople = InterleaveByGender(males, females);
        
        var familiesToUpdate = new List<Family>();
        var newMembersToAdd = new List<FamilyMember>();

        if (!cmd.ReplaceExisting && cmd.FillExistingFirst && existingFamilies.Count > 0)
        {
            var existingMembers = await familyMemberRepo.ListByRetreatAsync(cmd.RetreatId, ct);
            var membersByFamily = existingMembers.GroupBy(m => m.FamilyId).ToDictionary(g => g.Key, g => g.ToList());

            var incompleteFamilies = existingFamilies
                .Where(f => !f.IsLocked)
                .Select(f => new
                {
                    Family = f,
                    CurrentMembers = membersByFamily.GetValueOrDefault(f.Id, new List<FamilyMember>()),
                    CurrentCount = membersByFamily.GetValueOrDefault(f.Id, new List<FamilyMember>()).Count,
                    Remaining = f.Capacity - membersByFamily.GetValueOrDefault(f.Id, new List<FamilyMember>()).Count
                })
                .Where(x => x.Remaining > 0)
                .OrderBy(x => x.CurrentCount)
                .ToList();

            foreach (var incomplete in incompleteFamilies)
            {
                if (allPeople.Count == 0) break;

                var currentMemberIds = incomplete.CurrentMembers.Select(m => m.RegistrationId).ToHashSet();
                var currentPeople = incomplete.CurrentMembers
                    .Select(m => pool.FirstOrDefault(p => p.Id == m.RegistrationId))
                    .Where(p => p != null)
                    .Select(p => CreatePersonInfo(p!))
                    .ToList();

                var nextPosition = incomplete.CurrentMembers.Any()
                    ? incomplete.CurrentMembers.Max(m => m.Position) + 1
                    : 0;

                var added = 0;
                for (int i = 0; i < allPeople.Count && added < incomplete.Remaining; i++)
                {
                    var person = allPeople[i];
                    
                    // ✅ Tenta evitar conflitos críticos, mas se não conseguir, adiciona mesmo assim
                    var currentMales = currentPeople.Count(p => p.Gender == Gender.Male);
                    var currentFemales = currentPeople.Count - currentMales;
                    var maxAllowed = incomplete.Family.Capacity / 2;

                    if (person.Gender == Gender.Male && currentMales >= maxAllowed)
                        continue;
                    if (person.Gender == Gender.Female && currentFemales >= maxAllowed)
                        continue;
                    
                    newMembersToAdd.Add(new FamilyMember(
                        cmd.RetreatId,
                        incomplete.Family.Id,
                        person.Id,
                        position: nextPosition++,
                        isPadrinho: false,
                        isMadrinha: false
                    ));

                    currentPeople.Add(person);
                    allPeople.RemoveAt(i);
                    i--;
                    added++;
                }

                if (added > 0)
                    familiesToUpdate.Add(incomplete.Family);
            }
        }
        
        var totalPeople = allPeople.Count;

        if (totalPeople < 4)
        {
            if (newMembersToAdd.Count > 0)
            {
                await familyMemberRepo.AddRangeAsync(newMembersToAdd, ct);
                retreat.BumpFamiliesVersion();
                await retreatRepo.UpdateAsync(retreat, ct);
                await uow.SaveChangesAsync(ct);
                
                return await BuildResponseAsync(familiesToUpdate, newMembersToAdd, retreat.FamiliesVersion, ct);
            }
            
            return new GenerateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<GeneratedFamilyDto>());
        }

        var fullFamiliesCount = totalPeople / cmd.MembersPerFamily;
        var remainingPeople = totalPeople % cmd.MembersPerFamily;

        var familiesCount = fullFamiliesCount;
        if (remainingPeople >= 4)
        {
            familiesCount++;
        }

        var peopleToAllocate = remainingPeople >= 4 
            ? allPeople 
            : allPeople.Take(fullFamiliesCount * cmd.MembersPerFamily).ToList();

        allPeople = peopleToAllocate;

        var createdFamilies = new List<Family>();
        var createdMembers = new List<FamilyMember>();

        if (familiesCount > 0)
        {
            var usedColors = cmd.ReplaceExisting
                ? new List<string>()
                : await familyRepo.GetUsedColorsInRetreatAsync(cmd.RetreatId, ct);

            var availableColors = FamilyColor.AvailableColors
                .Where(c => !usedColors.Contains(c.Name, StringComparer.OrdinalIgnoreCase))
                .ToList();

            if (availableColors.Count < familiesCount)
                throw new BusinessRuleException($"Não há cores suficientes disponíveis. Necessário {familiesCount}, disponível {availableColors.Count}.");

            var startIndex = existingFamilies.Count + 1;
            var random = new Random();

            for (var i = 0; i < familiesCount; i++)
            {
                var colorIndex = random.Next(availableColors.Count);
                var color = availableColors[colorIndex];
                availableColors.RemoveAt(colorIndex);
                
                var family = new Family(
                    name: new FamilyName($"Família {startIndex + i}"),
                    retreatId: cmd.RetreatId,
                    capacity: cmd.MembersPerFamily, 
                    color: color
                );
                createdFamilies.Add(family);
            }

            var familySlots = createdFamilies.ToDictionary(f => f.Id, f => new List<PersonInfo>());

            // ✅ Distribui pessoas tentando minimizar alertas, mas prioriza alocar todos
            foreach (var person in allPeople)
            {
                var bestFamily = FindBestFamilyForPerson(person, createdFamilies, familySlots, cmd.MembersPerFamily);

                if (bestFamily != null)
                {
                    familySlots[bestFamily.Id].Add(person);
                }
                else
                {
                    // ✅ Se não encontrar "perfeita", pega qualquer uma com espaço
                    var anyWithSpace = createdFamilies.FirstOrDefault(f => familySlots[f.Id].Count < cmd.MembersPerFamily);
                    if (anyWithSpace != null)
                    {
                        familySlots[anyWithSpace.Id].Add(person);
                    }
                }
            }

            // ✅ Só persiste famílias com >= 4 membros
            foreach (var family in createdFamilies)
            {
                var members = familySlots[family.Id];
                
                // ✅ Ignora famílias com < 4 membros
                if (members.Count < 4)
                    continue;
                
                for (int pos = 0; pos < members.Count; pos++)
                {
                    var member = new FamilyMember(
                        cmd.RetreatId,
                        family.Id,
                        members[pos].Id,
                        position: pos,
                        isPadrinho: false,
                        isMadrinha: false
                    );
                    createdMembers.Add(member);
                }
            }
            
            // ✅ Remove famílias com < 4 membros da lista de persistência
            createdFamilies = createdFamilies
                .Where(f => familySlots[f.Id].Count >= 4)
                .ToList();
        }
        
        if (newMembersToAdd.Count > 0)
            await familyMemberRepo.AddRangeAsync(newMembersToAdd, ct);

        foreach (var f in createdFamilies)
            await familyRepo.AddAsync(f, ct);

        if (createdMembers.Count > 0)
            await familyMemberRepo.AddRangeAsync(createdMembers, ct);

        retreat.BumpFamiliesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);
        
        var allFamilies = cmd.FillExistingFirst && !cmd.ReplaceExisting
            ? familiesToUpdate.Concat(createdFamilies).ToList()
            : createdFamilies;

        var allMembers = cmd.FillExistingFirst && !cmd.ReplaceExisting
            ? newMembersToAdd.Concat(createdMembers).ToList()
            : createdMembers;

        var response = await BuildResponseAsync(allFamilies, allMembers, retreat.FamiliesVersion, ct);

        return response;
    }

    private async Task<List<Registration>> GetEligibleParticipantsAsync(Guid retreatId, bool replaceExisting, CancellationToken ct)
    {
        var confirmed = await registrationRepo.ListAsync(
            retreatId: retreatId,
            status: nameof(RegistrationStatus.Confirmed),
            region: null,
            skip: 0,
            take: int.MaxValue,
            ct: ct);

        var paymentConfirmed = await registrationRepo.ListAsync(
            retreatId: retreatId,
            status: nameof(RegistrationStatus.PaymentConfirmed),
            region: null,
            skip: 0,
            take: int.MaxValue,
            ct: ct);

        var pool = confirmed
            .Concat(paymentConfirmed)
            .Where(r => r.Enabled)
            .DistinctBy(r => r.Id)
            .ToList();

        if (!replaceExisting)
        {
            var alreadyAssigned = await familyMemberRepo.ListByRetreatAsync(retreatId, ct);
            var assignedIds = alreadyAssigned.Select(m => m.RegistrationId).ToHashSet();
            pool = pool.Where(r => !assignedIds.Contains(r.Id)).ToList();
        }

        return pool;
    }

    private PersonInfo CreatePersonInfo(Registration reg)
    {
        return new PersonInfo(
            reg.Id,
            NormalizeSurname(ExtractLastName((string)reg.Name)),
            (reg.City ?? "").Trim().ToLowerInvariant(),
            reg.Gender,
            reg.FatherName?.Trim().ToLowerInvariant(),
            reg.MotherName?.Trim().ToLowerInvariant(),
            reg.SubmitterNames?.Trim().ToLowerInvariant(),
            reg.HasRelativeOrFriendSubmitted == true,
            reg
        );
    }

    private List<PersonInfo> InterleaveByGender(List<PersonInfo> males, List<PersonInfo> females)
    {
        var result = new List<PersonInfo>();
        var random = new Random();

        males = males.OrderBy(_ => random.Next()).ToList();
        females = females.OrderBy(_ => random.Next()).ToList();

        int maxCount = Math.Max(males.Count, females.Count);
        for (int i = 0; i < maxCount; i++)
        {
            if (i < males.Count) result.Add(males[i]);
            if (i < females.Count) result.Add(females[i]);
        }

        return result;
    }

    private Family? FindBestFamilyForPerson(
        PersonInfo person,
        List<Family> families,
        Dictionary<Guid, List<PersonInfo>> familySlots,
        int capacity)
    {
        // ✅ NÍVEL 1: Família perfeita (sem conflitos e balanceamento de gênero)
        var perfect = families.FirstOrDefault(f =>
        {
            var members = familySlots[f.Id];
            if (members.Count >= capacity) return false; // ✅ NUNCA ultrapassa capacidade

            if (HasConflict(person, members)) return false;

            var sameGenderCount = members.Count(m => m.Gender == person.Gender);
            var maxAllowed = capacity / 2;
            if (sameGenderCount >= maxAllowed) return false;

            return true;
        });

        if (perfect != null) return perfect;

        // ✅ NÍVEL 2: Boa (sem conflitos críticos, pode ter warning)
        var good = families.FirstOrDefault(f =>
        {
            var members = familySlots[f.Id];
            if (members.Count >= capacity) return false; // ✅ NUNCA ultrapassa capacidade

            if (HasCriticalConflict(person, members)) return false;

            var sameGenderCount = members.Count(m => m.Gender == person.Gender);
            var maxAllowed = capacity / 2;
            if (sameGenderCount >= maxAllowed) return false;

            return true;
        });

        if (good != null) return good;

        // ✅ NÍVEL 3: Aceitável (pode ter balanceamento ruim, mas sem conflitos críticos)
        var acceptable = families.FirstOrDefault(f =>
        {
            var members = familySlots[f.Id];
            if (members.Count >= capacity) return false; // ✅ NUNCA ultrapassa capacidade

            if (HasCriticalConflict(person, members)) return false;

            return true;
        });

        if (acceptable != null) return acceptable;

        // ✅ NÍVEL 4: Qualquer família com espaço (mesmo com conflitos - vai gerar alertas)
        return families.FirstOrDefault(f => familySlots[f.Id].Count < capacity);
    }

    private bool HasConflict(PersonInfo person, List<PersonInfo> members)
    {
        return HasCriticalConflict(person, members) || HasWarningConflict(person, members);
    }

    private bool HasCriticalConflict(PersonInfo person, List<PersonInfo> members)
    {
        if (!string.IsNullOrWhiteSpace(person.NormalizedSurname))
        {
            if (members.Any(m => m.NormalizedSurname == person.NormalizedSurname))
                return true;
        }

        if (!string.IsNullOrWhiteSpace(person.FatherName) || !string.IsNullOrWhiteSpace(person.MotherName))
        {
            foreach (var m in members)
            {
                if (!string.IsNullOrWhiteSpace(person.FatherName) &&
                    !string.IsNullOrWhiteSpace(m.FatherName) &&
                    person.FatherName == m.FatherName)
                    return true;

                if (!string.IsNullOrWhiteSpace(person.MotherName) &&
                    !string.IsNullOrWhiteSpace(m.MotherName) &&
                    person.MotherName == m.MotherName)
                    return true;
            }
        }

        return false;
    }

    private bool HasWarningConflict(PersonInfo person, List<PersonInfo> members)
    {
        if (!string.IsNullOrWhiteSpace(person.City))
        {
            if (members.Any(m => m.City == person.City))
                return true;
        }

        return false;
    }

    private async Task<GenerateFamiliesResponse> BuildResponseAsync(
        List<Family> families,
        List<FamilyMember> members,
        int version,
        CancellationToken ct)
    {
        var allRegIds = members.Select(m => m.RegistrationId).Distinct().ToArray();
        var registrationsDict = await registrationRepo.GetMapByIdsAsync(allRegIds, ct) ?? new Dictionary<Guid, Registration>();

        var allFamiliesInRetreat = families.Count > 0 
            ? await familyRepo.ListByRetreatAsync(families[0].RetreatId, ct) 
            : new List<Family>();

        var allMembersInRetreat = families.Count > 0 
            ? await familyMemberRepo.ListByRetreatAsync(families[0].RetreatId, ct) 
            : new List<FamilyMember>();

        var sizesById = allMembersInRetreat.GroupBy(m => m.FamilyId).ToDictionary(g => g.Key, g => g.Count());

        var familyDtos = families.Select(f =>
        {
            var familyMembers = members
                .Where(m => m.FamilyId == f.Id)
                .OrderBy(m => m.Position)
                .Select(m =>
                {
                    var reg = registrationsDict.TryGetValue(m.RegistrationId, out var r) ? r : null;
                    return new GeneratedMemberDto(
                        m.RegistrationId,
                        reg is null ? string.Empty : (string)reg.Name,
                        reg?.Email?.Value ?? string.Empty,
                        reg?.Phone ?? string.Empty,
                        reg is null ? string.Empty : reg.Gender.ToString(),
                        reg?.City ?? string.Empty,
                        m.Position,
                        m.IsPadrinho,
                        m.IsMadrinha
                    );
                }).ToList();

            var maleCount = familyMembers.Count(x => x.Gender.Equals(nameof(Gender.Male), StringComparison.OrdinalIgnoreCase));
            var femaleCount = familyMembers.Count - maleCount;

            var registrations = familyMembers
                .Select(m => registrationsDict.GetValueOrDefault(m.RegistrationId))
                .Where(r => r != null)
                .Cast<Registration>()
                .ToList();

            var padrinhos = familyMembers.Where(m => m.IsPadrinho).Select(m => m.RegistrationId).ToList();
            var madrinhas = familyMembers.Where(m => m.IsMadrinha).Select(m => m.RegistrationId).ToList();

            var alerts = FamilyAlertsCalculator.CalculateWithGodparents(
                registrations,
                padrinhos,
                madrinhas,
                f.Capacity
            );

            var otherSizes = allFamiliesInRetreat
                .Where(of => of.Id != f.Id)
                .Select(of => sizesById.GetValueOrDefault(of.Id, 0))
                .Where(s => s > 0)
                .ToList();

            if (otherSizes.Count > 0)
            {
                var sizeAlerts = FamilyAlertsCalculator.CheckFamilySizeComparison(
                    familyMembers.Count,
                    otherSizes,
                    registrations.Select(r => r.Id).ToList()
                );
                alerts.AddRange(sizeAlerts);
            }

            return new GeneratedFamilyDto(
                f.Id,
                (string)f.Name,
                f.Color.Name,
                f.Color.HexCode,
                f.Capacity,
                familyMembers.Count,
                maleCount,
                femaleCount,
                f.Capacity - familyMembers.Count,
                familyMembers,
                alerts.Select(a => new FamilyAlertDto(
                    a.Severity,
                    a.Code,
                    a.Message,
                    a.RegistrationIds
                )).ToList()
            );
        }).ToList();

        return new GenerateFamiliesResponse(version, familyDtos);
    }

    private static string ExtractLastName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? string.Empty : parts[^1];
    }

    private static string NormalizeSurname(string surname)
    {
        if (string.IsNullOrWhiteSpace(surname)) return string.Empty;
        var s = surname.Trim().ToLowerInvariant();
        if (s is "de" or "da" or "do" or "dos" or "das") return string.Empty;
        return s;
    }
}
