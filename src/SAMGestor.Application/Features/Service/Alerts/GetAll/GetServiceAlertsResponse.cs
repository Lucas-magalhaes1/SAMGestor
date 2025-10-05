namespace SAMGestor.Application.Features.Service.Alerts.GetAll;

public sealed record GetServiceAlertsResponse(
    int Version,
    DateTime GeneratedAtUtc,
    IReadOnlyList<ServiceSpaceAlertView> Spaces
);

public sealed record ServiceSpaceAlertView(
    Guid   SpaceId,
    string Name,
    int    MinPeople,
    int    MaxPeople,
    int    AssignedCount,
    int    PreferenceCount,
    bool   HasCoordinator,
    bool   HasVice,
    IReadOnlyList<ServiceAlertItem> Alerts
);

public sealed record ServiceAlertItem(
    string Code,      // ex: PreferenceBelowMin, PreferenceOverMax, MissingCoordinator, MissingVice, BelowMin, OverMax, NoPreferences
    string Severity,  // "info" | "warning" | "error"
    string Message
);