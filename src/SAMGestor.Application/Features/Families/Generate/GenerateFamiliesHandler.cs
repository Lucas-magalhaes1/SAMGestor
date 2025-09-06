using MediatR;
using SAMGestor.Application.Interfaces;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.Generate;

public sealed class GenerateFamiliesHandler(
    IRetreatRepository retreatRepo,
    IRegistrationRepository registrationRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository familyMemberRepo,
    IUnitOfWork uow
) : IRequestHandler<GenerateFamiliesCommand, GenerateFamiliesResponse>
{
    private const int DefaultCapacity = 4;

    public async Task<GenerateFamiliesResponse> Handle(GenerateFamiliesCommand cmd, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(cmd.RetreatId, ct);
        if (retreat is null)
            throw new NotFoundException(nameof(Retreat), cmd.RetreatId);

        var capacity = cmd.Capacity ?? DefaultCapacity;
        if (capacity <= 0)
            throw new BusinessRuleException("Capacity inválido.");

        if (cmd.ReplaceExisting)
        {
            await familyMemberRepo.DeleteAllByRetreatAsync(cmd.RetreatId, ct);
            await familyRepo.DeleteAllByRetreatAsync(cmd.RetreatId, ct);
        }

        var confirmed = await registrationRepo.ListAsync(
            retreatId: cmd.RetreatId,
            status: nameof(RegistrationStatus.Confirmed),
            region: null,
            skip: 0, take: int.MaxValue,
            ct: ct);

        var paymentConfirmed = await registrationRepo.ListAsync(
            retreatId: cmd.RetreatId,
            status: nameof(RegistrationStatus.PaymentConfirmed),
            region: null,
            skip: 0, take: int.MaxValue,
            ct: ct);

        var pool = confirmed
            .Concat(paymentConfirmed)
            .Where(r => r.Enabled)
            .DistinctBy(r => r.Id)
            .ToList();

        if (!cmd.ReplaceExisting)
        {
            var alreadyAssigned = await familyMemberRepo.ListByRetreatAsync(cmd.RetreatId, ct);
            var assignedIds = alreadyAssigned.Select(m => m.RegistrationId).ToHashSet();
            pool = pool.Where(r => !assignedIds.Contains(r.Id)).ToList();
        }

        if (pool.Count == 0)
            return new GenerateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<GeneratedFamilyDto>());

        var males = pool.Where(p => p.Gender == Gender.Male).OrderBy(_ => Guid.NewGuid()).ToList();
        var females = pool.Where(p => p.Gender == Gender.Female).OrderBy(_ => Guid.NewGuid()).ToList();

        var familiesCount = Math.Min(males.Count / 2, females.Count / 2);
        if (familiesCount <= 0)
            return new GenerateFamiliesResponse(retreat.FamiliesVersion, Array.Empty<GeneratedFamilyDto>());

        var createdFamilies = new List<Family>(familiesCount);
        var createdMembers = new List<FamilyMember>(familiesCount * capacity);

        for (var i = 1; i <= familiesCount; i++)
        {
            var fam = new Family(
                name: new Domain.ValueObjects.FullName($"Família {i}"),
                retreatId: cmd.RetreatId,
                capacity: capacity);

            await familyRepo.AddAsync(fam, ct);
            createdFamilies.Add(fam);

            for (int k = 0; k < 2 && males.Count > 0; k++)
            {
                var p = males[0];
                males.RemoveAt(0);
                var pos = createdMembers.Count(m => m.FamilyId == fam.Id);
                createdMembers.Add(new FamilyMember(cmd.RetreatId, fam.Id, p.Id, position: pos));
            }

            for (int k = 0; k < 2 && females.Count > 0; k++)
            {
                var p = females[0];
                females.RemoveAt(0);
                var pos = createdMembers.Count(m => m.FamilyId == fam.Id);
                createdMembers.Add(new FamilyMember(cmd.RetreatId, fam.Id, p.Id, position: pos));
            }
        }

        foreach (var m in createdMembers)
            await familyMemberRepo.AddAsync(m, ct);

        retreat.BumpFamiliesVersion();
        await retreatRepo.UpdateAsync(retreat, ct);
        await uow.SaveChangesAsync(ct);

        var registrationsDict = pool.ToDictionary(r => r.Id, r => r);

        var familyDtos = createdFamilies.Select(f =>
        {
            var members = createdMembers
                .Where(m => m.FamilyId == f.Id)
                .OrderBy(m => m.Position)
                .Select(m =>
                {
                    var reg = registrationsDict[m.RegistrationId];
                    return new GeneratedMemberDto(
                        reg.Id,
                        (string)reg.Name,
                        reg.Gender.ToString(),
                        reg.City,
                        m.Position
                    );
                }).ToList();

            var maleCount = members.Count(x => x.Gender.Equals(nameof(Gender.Male), StringComparison.OrdinalIgnoreCase));
            var femaleCount = members.Count - maleCount;
            var alerts = CalculateAlerts(members);

            return new GeneratedFamilyDto(
                f.Id,
                (string)f.Name,
                f.Capacity,
                members.Count,
                maleCount,
                femaleCount,
                f.Capacity - members.Count,
                members,
                alerts
            );
        }).ToList();

        return new GenerateFamiliesResponse(retreat.FamiliesVersion, familyDtos);
    }

    private static List<FamilyAlertDto> CalculateAlerts(List<GeneratedMemberDto> members)
    {
        var alerts = new List<FamilyAlertDto>();

        var surnameGroups = members
            .Select(m => new { m, Last = ExtractLastName(m.Name) })
            .GroupBy(x => NormalizeSurname(x.Last))
            .Where(g => g.Key.Length > 0 && g.Count() > 1)
            .ToList();

        foreach (var g in surnameGroups)
        {
            alerts.Add(new FamilyAlertDto(
                "critical",
                "SAME_SURNAME",
                $"Sobrenome repetido na família: '{g.Key}'.",
                g.Select(x => x.m.RegistrationId).ToList()
            ));
        }

        var cityGroups = members
            .GroupBy(m => (m.City ?? string.Empty).Trim().ToLowerInvariant())
            .Where(g => g.Key.Length > 0 && g.Count() > 1);

        foreach (var g in cityGroups)
        {
            alerts.Add(new FamilyAlertDto(
                "warning",
                "SAME_CITY",
                $"Múltiplos membros da mesma cidade: '{g.Key}'.",
                g.Select(x => x.RegistrationId).ToList()
            ));
        }

        return alerts;
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
