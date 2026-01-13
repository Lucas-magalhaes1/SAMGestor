using MediatR;
using SAMGestor.Application.Common.Families;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.GetAll;

public sealed class GetAllFamiliesHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository familyMemberRepo,
    IRegistrationRepository registrationRepo
) : IRequestHandler<GetAllFamiliesQuery, GetAllFamiliesResponse>
{
    public async Task<GetAllFamiliesResponse> Handle(GetAllFamiliesQuery query, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(query.RetreatId, ct);
        if (retreat is null)
            return new GetAllFamiliesResponse(0, false, Array.Empty<FamilyDto>());

        var families = await familyRepo.ListByRetreatAsync(query.RetreatId, ct);
        if (families.Count == 0)
            return new GetAllFamiliesResponse(retreat.FamiliesVersion, retreat.FamiliesLocked, Array.Empty<FamilyDto>());

        var familyIds = families.Select(f => f.Id).ToArray();
        var membersByFamily = await familyMemberRepo.ListByFamilyIdsAsync(familyIds, ct);
        
        var allRegIds = membersByFamily.Values
            .SelectMany(l => l)
            .Select(m => m.RegistrationId)
            .Distinct()
            .ToArray();
        
        var regsMap = await registrationRepo.GetMapByIdsAsync(allRegIds, ct);

        var sizesById = membersByFamily.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);

        var familyDtos = new List<FamilyDto>(families.Count);

        foreach (var f in families)
        {
            membersByFamily.TryGetValue(f.Id, out var members);
            members ??= new List<FamilyMember>();

            var memberDtos = members
                .OrderBy(m => m.Position)
                .Select(m =>
                {
                    var reg = regsMap.TryGetValue(m.RegistrationId, out var r) ? r : null;
                    return new MemberDto(
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
                })
                .ToList();

            var maleCount = memberDtos.Count(m => m.Gender.Equals("Male", StringComparison.OrdinalIgnoreCase));
            var femaleCount = memberDtos.Count - maleCount;
            var totalMembers = memberDtos.Count;
            var remaining = Math.Max(0, f.Capacity - totalMembers);

            var malePercentage = totalMembers > 0 ? (maleCount * 100.0m / totalMembers) : 0m;
            var femalePercentage = totalMembers > 0 ? (femaleCount * 100.0m / totalMembers) : 0m;

            var alerts = new List<FamilyAlertDto>();
            if (query.IncludeAlerts)
            {
                var registrations = members
                    .Select(m => regsMap.TryGetValue(m.RegistrationId, out var r) ? r : null)
                    .Where(r => r != null)
                    .Cast<Registration>()
                    .ToList();

                var padrinhos = members.Where(m => m.IsPadrinho).Select(m => m.RegistrationId).ToList();
                var madrinhas = members.Where(m => m.IsMadrinha).Select(m => m.RegistrationId).ToList();

                var calculatedAlerts = FamilyAlertsCalculator.CalculateWithGodparents(
                    registrations,
                    padrinhos,
                    madrinhas,
                    f.Capacity
                );

                var otherSizes = families
                    .Where(of => of.Id != f.Id)
                    .Select(of => sizesById.GetValueOrDefault(of.Id, 0))
                    .Where(s => s > 0)
                    .ToList();

                if (otherSizes.Count > 0)
                {
                    var sizeAlerts = FamilyAlertsCalculator.CheckFamilySizeComparison(
                        totalMembers,
                        otherSizes,
                        registrations.Select(r => r.Id).ToList()
                    );
                    calculatedAlerts.AddRange(sizeAlerts);
                }

                alerts = calculatedAlerts.Select(a => new FamilyAlertDto(
                    a.Severity,
                    a.Code,
                    a.Message,
                    a.RegistrationIds
                )).ToList();
            }

            var dto = new FamilyDto(
                f.Id,
                (string)f.Name,
                f.Color.Name,
                f.Color.HexCode,
                f.IsLocked,
                f.Capacity,
                totalMembers,
                maleCount,
                femaleCount,
                malePercentage,
                femalePercentage,
                remaining,
                memberDtos,
                alerts
            );

            familyDtos.Add(dto);
        }

        return new GetAllFamiliesResponse(retreat.FamiliesVersion, retreat.FamiliesLocked, familyDtos);
    }
}
