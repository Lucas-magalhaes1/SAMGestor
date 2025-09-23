using MediatR;

namespace SAMGestor.Application.Features.Families.Groups.Status;

public sealed record GetGroupsStatusSummaryQuery(Guid RetreatId) : IRequest<GetGroupsStatusSummaryResponse>;

public sealed record GetGroupsStatusSummaryResponse(
    int TotalFamilies,
    int None,
    int Creating,
    int Active,
    int Failed
);