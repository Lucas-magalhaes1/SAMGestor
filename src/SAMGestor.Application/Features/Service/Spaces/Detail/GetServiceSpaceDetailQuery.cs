using MediatR;

namespace SAMGestor.Application.Features.Service.Spaces.Detail;

public sealed record GetServiceSpaceDetailQuery(
    Guid RetreatId,
    Guid SpaceId,
    int  Page = 1,
    int  PageSize = 50,
    string? Search = null
) : IRequest<GetServiceSpaceDetailResponse>;

public sealed record GetServiceSpaceDetailResponse(
    int Version,
    SpaceView Space,
    int TotalMembers,
    int Page,
    int PageSize,
    IReadOnlyList<MemberItem> Members
);

public sealed record SpaceView(
    Guid   SpaceId,
    string Name,
    string? Description,
    bool   IsActive,
    bool   IsLocked,
    int    MinPeople,
    int    MaxPeople,
    bool   HasCoordinator,
    bool   HasVice,
    int    Allocated
);

public sealed record MemberItem(
    Guid RegistrationId,
    string Name,
    string Email,
    string Cpf,
    string Role
);