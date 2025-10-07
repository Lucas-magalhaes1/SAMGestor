using MediatR;

namespace SAMGestor.Application.Features.Service.Spaces.BulkCapacity;

public sealed record UpdateServiceSpacesCapacityCommand(
    Guid RetreatId,
    bool ApplyToAll,
    int? MinPeople,
    int? MaxPeople,
    IReadOnlyList<UpdateServiceSpacesCapacityCommand.Item>? Items 
) : IRequest<UpdateServiceSpacesCapacityResponse>
{
    public sealed record Item(Guid SpaceId, int MinPeople, int MaxPeople);
}

public sealed record UpdateServiceSpacesCapacityResponse(
    int Version,
    int UpdatedCount,
    IReadOnlyList<Guid> SkippedLocked
);