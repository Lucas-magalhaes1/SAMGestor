using MediatR;

namespace SAMGestor.Application.Features.Service.Spaces.List;

public sealed record ListServiceSpacesQuery(
    Guid RetreatId,
    bool? IsActive = null,
    bool? IsLocked = null,
    string? Search = null
) : IRequest<ListServiceSpacesResponse>;

public sealed record ListServiceSpacesResponse(
    int Version,
    IReadOnlyList<ListItem> Items
);

public sealed record ListItem(
    Guid   SpaceId,
    string Name,
    string? Description,
    bool   IsActive,
    bool   IsLocked,
    int    MinPeople,
    int    MaxPeople,
    int    Allocated 
);