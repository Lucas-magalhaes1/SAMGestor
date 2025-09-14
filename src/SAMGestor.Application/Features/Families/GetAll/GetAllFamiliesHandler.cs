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

        var familyDtos = new List<FamilyDto>(families.Count);

        foreach (var f in families)
        {
            membersByFamily.TryGetValue(f.Id, out var members);
            members ??= new List<FamilyMember>();

            var memberViews = members
                .OrderBy(m => m.Position)
                .Select(m =>
                {
                    var reg = regsMap[m.RegistrationId];
                    return new FamilyRead.MemberView(
                        reg.Id,
                        (string)reg.Name,
                        reg.Gender,
                        reg.City,
                        m.Position
                    );
                })
                .ToList();

            var (total, male, female, remaining) = FamilyRead.Metrics(f.Capacity, memberViews);
            var alerts = query.IncludeAlerts
                ? FamilyRead.Alerts(memberViews).Select(a =>
                    new FamilyAlertDto(a.Severity, a.Code, a.Message, a.RegistrationIds)).ToList()
                : new List<FamilyAlertDto>();

            var dto = new FamilyDto(
                f.Id,
                (string)f.Name,
                f.Capacity,
                total,
                male,
                female,
                remaining,
                memberViews.Select(v => new MemberDto(
                    v.RegistrationId, v.Name, v.Gender.ToString(), v.City, v.Position)).ToList(),
                alerts
            );

            familyDtos.Add(dto);
        }

        return new GetAllFamiliesResponse(retreat.FamiliesVersion, retreat.FamiliesLocked, familyDtos);
    }
}
