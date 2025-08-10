using MediatR;

namespace SAMGestor.Application.Features.Lottery;

public sealed record LotteryPreviewQuery(Guid RetreatId) : IRequest<LotteryResultDto>;