using MediatR;
using SAMGestor.Application.Common.Families;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        var regIds  = members.Select(m => m.RegistrationId).Distinct().ToArray();
        var regsMap = await registrationRepo.GetMapByIdsAsync(regIds, ct);

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

        var (total, male, female, remaining) = FamilyRead.Metrics(family.Capacity, memberViews);
        var alerts = query.IncludeAlerts
            ? FamilyRead.Alerts(memberViews)
            : new System.Collections.Generic.List<FamilyRead.AlertView>();

        var dto = new FamilyDto(
            family.Id,
            (string)family.Name,
            family.Capacity,
            total,
            male,
            female,
            remaining,
            memberViews.Select(v => new MemberDto(
                v.RegistrationId, v.Name, v.Gender.ToString(), v.City, v.Position)).ToList(),
            alerts.Select(a => new FamilyAlertDto(a.Severity, a.Code, a.Message, a.RegistrationIds)).ToList()
        );

        return new GetFamilyByIdResponse(retreat.FamiliesVersion, dto);
    }
}
