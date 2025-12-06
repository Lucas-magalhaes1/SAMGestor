public sealed record EmailChangedNotificationV1(
    Guid UserId,
    string OldEmail,
    string NewEmail,
    string ChangedBy
);