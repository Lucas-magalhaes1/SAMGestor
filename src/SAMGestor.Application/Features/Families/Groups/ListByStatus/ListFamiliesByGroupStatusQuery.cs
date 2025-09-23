using MediatR;

namespace SAMGestor.Application.Features.Families.Groups.ListByStatus;

public sealed record ListFamiliesByGroupStatusQuery(Guid RetreatId, string? Status )
    : IRequest<ListFamiliesByGroupStatusResponse>;

public sealed record ListFamiliesByGroupStatusResponse(IReadOnlyList<FamilyGroupItem> Items);

public sealed record FamilyGroupItem(
    Guid FamilyId,
    string Name,
    string GroupStatus,
    string? GroupLink,
    string? GroupExternalId,
    string? GroupChannel,
    DateTimeOffset? GroupCreatedAt,
    DateTimeOffset? GroupLastNotifiedAt,
    int GroupVersion
);