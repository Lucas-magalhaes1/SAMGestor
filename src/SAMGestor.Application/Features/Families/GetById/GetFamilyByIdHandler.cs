using MediatR;
using SAMGestor.Application.Common.Families;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Families.GetById;

public sealed class GetFamilyByIdHandler(
    IRetreatRepository retreatRepo,
    IFamilyRepository familyRepo,
    IFamilyMemberRepository familyMemberRepo,
    IRegistrationRepository registrationRepo
) : IRequestHandler<GetFamilyByIdQuery, GetFamilyByIdResponse>
{
    public async Task<GetFamilyByIdResponse> Handle(GetFamilyByIdQuery query, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(query.RetreatId, ct);
        if (retreat is null)
            return new GetFamilyByIdResponse(0, null);

        var family = await familyRepo.GetByIdAsync(query.FamilyId, ct);
        if (family is null || family.RetreatId != query.RetreatId)
            return new GetFamilyByIdResponse(retreat.FamiliesVersion, null);

        var members = await familyMemberRepo.ListByFamilyAsync(family.Id, ct);
        var regIds = members.Select(m => m.RegistrationId).Distinct().ToArray();
        var regsMap = await registrationRepo.GetMapByIdsAsync(regIds, ct);
        
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
        var remaining = Math.Max(0, family.Capacity - totalMembers);

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
                family.Capacity
            );

            var allFamilies = await familyRepo.ListByRetreatAsync(query.RetreatId, ct);
            if (allFamilies.Count > 1)
            {
                var allMembersInRetreat = await familyMemberRepo.ListByRetreatAsync(query.RetreatId, ct);
                var sizesById = allMembersInRetreat.GroupBy(m => m.FamilyId).ToDictionary(g => g.Key, g => g.Count());
                var otherSizes = allFamilies
                    .Where(f => f.Id != family.Id)
                    .Select(f => sizesById.GetValueOrDefault(f.Id, 0))
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
            }

            alerts = calculatedAlerts.Select(a => new FamilyAlertDto(
                a.Severity,
                a.Code,
                a.Message,
                a.RegistrationIds
            )).ToList();
        }

        var dto = new FamilyDto(
            FamilyId: family.Id,
            Name: (string)family.Name,
            ColorName: family.Color.Name,
            ColorHex: family.Color.HexCode,
            Capacity: family.Capacity,
            TotalMembers: totalMembers,
            MaleCount: maleCount,
            FemaleCount: femaleCount,
            MalePercentage: malePercentage,
            FemalePercentage: femalePercentage,
            Remaining: remaining,
            IsLocked: family.IsLocked,
            GroupStatus: family.GroupStatus.ToString(),
            GroupLink: family.GroupLink,
            GroupExternalId: family.GroupExternalId,
            GroupChannel: family.GroupChannel,
            GroupCreatedAt: family.GroupCreatedAt,
            GroupLastNotifiedAt: family.GroupLastNotifiedAt,
            GroupVersion: family.GroupVersion,
            Members: memberDtos,
            Alerts: alerts
        );

        return new GetFamilyByIdResponse(retreat.FamiliesVersion, dto);
    }
}
