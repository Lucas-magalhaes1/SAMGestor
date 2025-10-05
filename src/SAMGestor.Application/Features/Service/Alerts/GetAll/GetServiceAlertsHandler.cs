using MediatR;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Exceptions;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Alerts.GetAll;

public sealed class GetServiceAlertsHandler(
    IRetreatRepository retreatRepo,
    IServiceSpaceRepository spaceRepo,
    IServiceAssignmentRepository assignmentRepo,
    IServiceRegistrationRepository registrationRepo
) : IRequestHandler<GetServiceAlertsQuery, GetServiceAlertsResponse>
{
    public async Task<GetServiceAlertsResponse> Handle(GetServiceAlertsQuery q, CancellationToken ct)
    {
        var retreat = await retreatRepo.GetByIdAsync(q.RetreatId, ct)
                     ?? throw new NotFoundException(nameof(Retreat), q.RetreatId);

        var spaces = await spaceRepo.ListByRetreatAsync(q.RetreatId, ct);
        if (spaces.Count == 0)
        {
            return new GetServiceAlertsResponse(
                Version: retreat.ServiceSpacesVersion,
                GeneratedAtUtc: DateTime.UtcNow,
                Spaces: Array.Empty<ServiceSpaceAlertView>());
        }

        var assignments = await assignmentRepo.ListByRetreatAsync(q.RetreatId, ct);

        var bySpaceAssigned = assignments
            .GroupBy(a => a.ServiceSpaceId)
            .ToDictionary(g => g.Key, g => new
            {
                Count = g.Count(),
                HasCoord = g.Any(x => x.Role == ServiceRole.Coordinator),
                HasVice  = g.Any(x => x.Role == ServiceRole.Vice)
            });

        var prefCounts = await registrationRepo.CountPreferencesBySpaceAsync(q.RetreatId, ct);

        var results = new List<ServiceSpaceAlertView>(spaces.Count);

        foreach (var s in spaces)
        {
            var alerts = new List<ServiceAlertItem>();

            // --- assigned info
            int assignedCount = 0;
            bool hasCoord = false, hasVice = false;
            if (bySpaceAssigned.TryGetValue(s.Id, out var info))
            {
                assignedCount = info.Count;
                hasCoord = info.HasCoord;
                hasVice  = info.HasVice;
            }

            // --- preferences count
            int prefCount = 0;
            if (!prefCounts.TryGetValue(s.Id, out prefCount))
                prefCount = 0;

            // === Preferências (pré) ===
            if (q.Mode is ServiceAlertMode.All or ServiceAlertMode.Preferences)
            {
                if (prefCount == 0)
                {
                    alerts.Add(new ServiceAlertItem(
                        Code: "NoPreferences",
                        Severity: "info",
                        Message: string.Format("Nenhuma preferência registrada para '{0}'.", s.Name)
                    ));
                }
                else
                {
                    if (prefCount < s.MinPeople)
                    {
                        alerts.Add(new ServiceAlertItem(
                            Code: "PreferenceBelowMin",
                            Severity: "warning",
                            Message: string.Format("Preferências ({0}) abaixo do mínimo ({1}) em '{2}'.", prefCount, s.MinPeople, s.Name)
                        ));
                    }
                    if (prefCount > s.MaxPeople)
                    {
                        alerts.Add(new ServiceAlertItem(
                            Code: "PreferenceOverMax",
                            Severity: "warning",
                            Message: string.Format("Preferências ({0}) acima do máximo ({1}) em '{2}'.", prefCount, s.MaxPeople, s.Name)
                        ));
                    }
                }
            }

            // === Roster/Alocações (pós) ===
            if (q.Mode is ServiceAlertMode.All or ServiceAlertMode.Roster)
            {
                if (!hasCoord)
                {
                    alerts.Add(new ServiceAlertItem(
                        Code: "MissingCoordinator",
                        Severity: "warning",
                        Message: string.Format("Espaço '{0}' sem Coordenador.", s.Name)
                    ));
                }
                if (!hasVice)
                {
                    alerts.Add(new ServiceAlertItem(
                        Code: "MissingVice",
                        Severity: "warning",
                        Message: string.Format("Espaço '{0}' sem Vice.", s.Name)
                    ));
                }
                if (assignedCount < s.MinPeople)
                {
                    alerts.Add(new ServiceAlertItem(
                        Code: "BelowMin",
                        Severity: "warning",
                        Message: string.Format("Alocados ({0}) abaixo do mínimo ({1}) em '{2}'.", assignedCount, s.MinPeople, s.Name)
                    ));
                }
                if (assignedCount > s.MaxPeople)
                {
                    alerts.Add(new ServiceAlertItem(
                        Code: "OverMax",
                        Severity: "warning",
                        Message: string.Format("Alocados ({0}) acima do máximo ({1}) em '{2}'.", assignedCount, s.MaxPeople, s.Name)
                    ));
                }
            }

            if (alerts.Count == 0) continue;

            results.Add(new ServiceSpaceAlertView(
                SpaceId: s.Id,
                Name: s.Name,
                MinPeople: s.MinPeople,
                MaxPeople: s.MaxPeople,
                AssignedCount: assignedCount,
                PreferenceCount: prefCount,
                HasCoordinator: hasCoord,
                HasVice: hasVice,
                Alerts: alerts
            ));
        }

        return new GetServiceAlertsResponse(
            Version: retreat.ServiceSpacesVersion,
            GeneratedAtUtc: DateTime.UtcNow,
            Spaces: results
        );
    }
}
