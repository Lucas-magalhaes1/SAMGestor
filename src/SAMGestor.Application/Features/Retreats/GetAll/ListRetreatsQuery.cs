using MediatR;

namespace SAMGestor.Application.Features.Retreats.GetAll;

public record ListRetreatsQuery(int Skip = 0, int Take = 20)
    : IRequest<ListRetreatsResponse>;