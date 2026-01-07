using MediatR;
using SAMGestor.Application.Common.Pagination;

namespace SAMGestor.Application.Features.Service.Spaces.Detail;

public sealed record GetServiceSpaceDetailQuery(
    Guid RetreatId,
    Guid SpaceId,
    string? Search = null,
    int Skip = 0,
    int Take = 50
) : IRequest<GetServiceSpaceDetailResponse>;

public sealed record GetServiceSpaceDetailResponse(
    int Version,
    SpaceView Space,
    PagedResult<MemberItem> Members
);

public sealed record SpaceView(
    Guid SpaceId,
    string Name,
    string? Description,
    bool IsActive,
    bool IsLocked,
    int MinPeople,
    int MaxPeople,
    bool HasCoordinator,
    bool HasVice,
    int Allocated
);

public sealed record MemberItem(
    Guid RegistrationId,
    string Name,
    string Email,
    string Cpf,
    string Role
);