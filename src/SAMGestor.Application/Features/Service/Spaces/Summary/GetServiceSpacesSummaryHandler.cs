using MediatR;
using SAMGestor.Application.Dtos;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Application.Features.Service.Spaces.Summary;

public sealed class GetServiceSpacesSummaryHandler(
    IServiceSpaceRepository spaceRepo,
    IServiceRegistrationRepository regRepo
) : IRequestHandler<GetServiceSpacesSummaryQuery, IReadOnlyList<ServiceSpaceSummaryDto>>
{
    public async Task<IReadOnlyList<ServiceSpaceSummaryDto>> Handle(
        GetServiceSpacesSummaryQuery request, CancellationToken ct)
    {
        var spaces = await spaceRepo.ListByRetreatAsync(request.RetreatId, ct);

        var prefCounts = await regRepo.CountPreferencesBySpaceAsync(request.RetreatId, ct);

        SpaceLoadAlert GetAlert(int min, int max, int count)
            => count < min ? SpaceLoadAlert.BelowMin
                : count > max ? SpaceLoadAlert.OverMax
                : SpaceLoadAlert.WithinRange;

        return spaces
            .OrderBy(s => s.Name)
            .Select(s =>
            {
                var count = prefCounts.TryGetValue(s.Id, out var c) ? c : 0;
                var alert = GetAlert(s.MinPeople, s.MaxPeople, count);
                return new ServiceSpaceSummaryDto(
                    s.Id, s.Name, s.Description, s.MinPeople, s.MaxPeople, s.IsActive, count, alert
                );
            })
            .ToArray();
    }
}