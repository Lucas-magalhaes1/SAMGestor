namespace SAMGestor.Application.Features.Service.Spaces.Create;

public sealed record CreateServiceSpaceRequest(
    string Name,
    string? Description,
    int MinPeople,
    int MaxPeople,
    bool IsActive
);