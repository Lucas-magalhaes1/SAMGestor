namespace SAMGestor.Application.Dtos;

public sealed record ServiceSpacePublicDto(
    Guid   Id,
    string Name,
    string? Description
);