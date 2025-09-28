using MediatR;
using SAMGestor.Application.Dtos;

namespace SAMGestor.Application.Features.Service.Spaces.Summary;

public sealed record GetServiceSpacesSummaryQuery(Guid RetreatId)
    : IRequest<IReadOnlyList<ServiceSpaceSummaryDto>>;