using MediatR;

namespace SAMGestor.Application.Features.Families.Groups.RetryFailed;

public sealed record RetryFailedGroupsCommand(Guid RetreatId, bool AlsoNotify = true)
    : IRequest<RetryFailedGroupsResponse>;

public sealed record RetryFailedGroupsResponse(int TotalFailed, int Queued, int Skipped);