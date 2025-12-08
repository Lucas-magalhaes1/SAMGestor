using MediatR;

namespace SAMGestor.Application.Features.Lottery;

public sealed record LotteryCommitCommand(
    Guid RetreatId,
    List<string>? PriorityCities = null,
    int? MinAge = null,
    int? MaxAge = null
) : IRequest<LotteryResultDto>;