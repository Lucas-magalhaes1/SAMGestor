using MediatR;

namespace SAMGestor.Application.Features.Lottery;

public sealed record LotteryCommitCommand(Guid RetreatId) : IRequest<LotteryResultDto>;