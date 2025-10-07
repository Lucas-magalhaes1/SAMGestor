namespace SAMGestor.Application.Features.Service.Spaces.BulkCapacity;

public sealed record UpdateServiceSpacesCapacityRequest(
    bool ApplyToAll,
    int? MinPeople,
    int? MaxPeople,
    IReadOnlyList<UpdateServiceSpacesCapacityItem>? Items
);

public sealed record UpdateServiceSpacesCapacityItem(
    Guid SpaceId,
    int MinPeople,
    int MaxPeople
);