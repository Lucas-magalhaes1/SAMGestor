using MediatR;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Tents.List;

public sealed record ListTentsQuery(
    Guid RetreatId,
    TentCategory? Category = null,
    bool? Active = null
) : IRequest<IReadOnlyList<TentListItem>>;