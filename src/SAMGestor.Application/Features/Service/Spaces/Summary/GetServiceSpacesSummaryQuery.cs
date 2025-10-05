using MediatR;

namespace SAMGestor.Application.Features.Service.Spaces.Summary;

public sealed record GetServiceSpacesSummaryQuery(Guid RetreatId)
    : IRequest<GetServiceSpacesSummaryResponse>;

public sealed record GetServiceSpacesSummaryResponse(
    int Version,
    IReadOnlyList<SpaceSummaryItem> Items
);

public sealed record SpaceSummaryItem(
    Guid   SpaceId,
    string Name,
    bool   IsActive,
    bool   IsLocked,
    int    MinPeople,
    int    MaxPeople,
    int    Allocated,      
    int    PreferredCount,  
    bool   HasCoordinator,
    bool   HasVice
);