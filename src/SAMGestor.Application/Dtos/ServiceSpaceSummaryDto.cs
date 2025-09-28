namespace SAMGestor.Application.Dtos;

public enum SpaceLoadAlert
{
    BelowMin,
    WithinRange,
    OverMax
}

public sealed record ServiceSpaceSummaryDto(
    Guid   Id,
    string Name,
    string? Description,
    int MinPeople,
    int MaxPeople,
    bool IsActive,
    int PreferencesCount,
    SpaceLoadAlert Alert
);