using MediatR;

namespace SAMGestor.Application.Features.Dev.Seed;

public record ClearSeedDataCommand : IRequest<ClearSeedDataResult>;

public record ClearSeedDataResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int RetreatsDeleted { get; init; }
    public int RegistrationsDeleted { get; init; }
    public int ServiceRegistrationsDeleted { get; init; }
    public int ServiceSpacesDeleted { get; init; }
    public int FamiliesDeleted { get; init; }
    public int TentsDeleted { get; init; }
}