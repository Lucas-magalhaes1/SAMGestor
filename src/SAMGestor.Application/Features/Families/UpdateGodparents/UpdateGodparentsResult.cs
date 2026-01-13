namespace SAMGestor.Application.Features.Families.UpdateGodparents;

public sealed record UpdateGodparentsResult(
    bool Success,
    int Version,
    IReadOnlyList<string> Warnings  
);