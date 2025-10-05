using MediatR;

namespace SAMGestor.Application.Features.Service.Spaces.PublicList;

public sealed record PublicListServiceSpacesQuery(Guid RetreatId)
    : IRequest<PublicListServiceSpacesResponse>;

public sealed record PublicListServiceSpacesResponse(
    int Version,
    IReadOnlyList<PublicItem> Items
);

public sealed record PublicItem(Guid Id, string Name, string? Description);