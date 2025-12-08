using MediatR;

namespace SAMGestor.Application.Features.Lottery;

public sealed record LotteryPreviewQuery(
    Guid RetreatId,
    List<string>? PriorityCities = null,  // Ex: ["Recife", "São Paulo"]
    int? MinAge = null,                    // Idade mínima para prioridade (ex: 18)
    int? MaxAge = null                     // Idade máxima para prioridade (ex: 25)
) : IRequest<LotteryResultDto>;