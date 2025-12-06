using System.Text.Json.Serialization;

namespace SAMGestor.Contracts;

public static class EventTypes
{
    public const string SelectionParticipantSelectedV1 = "selection.participant.selected.v1";
    public const string SelectionParticipantSelectedV2 = "selection.participant.selected.v2"; 

    public const string NotificationEmailSentV1   = "notification.email.sent.v1";
    public const string NotificationEmailFailedV1 = "notification.email.failed.v1";

    public const string PaymentRequestedV1   = "payment.requested.v1";
    public const string PaymentLinkCreatedV1 = "payment.link.created.v1";
    public const string PaymentConfirmedV1   = "payment.confirmed.v1";
    
    public const string FamilyGroupCreateRequestedV1 = "family.group.create.requested.v1";
    public const string FamilyGroupCreatedV1         = "family.group.created.v1";
    public const string FamilyGroupCreateFailedV1    = "family.group.create.failed.v1";
    public const string FamilyGroupNotifyRequestedV1 = "family.group.notify.requested.v1";

    public const string ServingParticipantSelectedV1 = "serving.participant.selected.v1";
    
    public const string UserInvitedV1 = "user.invited.v1";
    public const string PasswordResetRequestedV1 = "user.password.reset.requested.v1";
    public const string EmailChangedByAdminV1 = "user.email.changed.by.admin.v1";
    public const string EmailChangedNotificationV1 = "user.email.changed.notification.v1";
    public const string PasswordChangedByAdminV1 = "user.password.changed.by.admin.v1";

}

public sealed record EventEnvelope<T>(
    [property: JsonPropertyName("id")]          string Id,
    [property: JsonPropertyName("type")]        string Type,
    [property: JsonPropertyName("source")]      string Source,
    [property: JsonPropertyName("time")]        DateTimeOffset Time,
    [property: JsonPropertyName("traceId")]     string TraceId,
    [property: JsonPropertyName("specversion")] string SpecVersion,
    [property: JsonPropertyName("data")]        T Data
)
{
    public static EventEnvelope<T> Create(string type, string source, T data, string? traceId = null)
        => new(Guid.NewGuid().ToString(), type, source, DateTimeOffset.UtcNow, traceId ?? Guid.NewGuid().ToString(), "1.0", data);
}