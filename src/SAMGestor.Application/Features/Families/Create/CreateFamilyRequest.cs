public sealed record CreateFamilyRequest(
    string? Name,
    IReadOnlyList<Guid> MemberIds,
    bool IgnoreWarnings = false
);