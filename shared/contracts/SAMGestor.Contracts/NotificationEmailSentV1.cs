using System.Text.Json.Serialization;

namespace SAMGestor.Contracts;

public sealed record NotificationEmailSentV1(
    [property: JsonPropertyName("notificationId")] Guid NotificationId,
    [property: JsonPropertyName("registrationId")] Guid RegistrationId,
    [property: JsonPropertyName("email")]          string Email,
    [property: JsonPropertyName("sentAt")]         DateTimeOffset SentAt
);

public sealed record NotificationEmailFailedV1(
    [property: JsonPropertyName("notificationId")] Guid NotificationId,
    [property: JsonPropertyName("registrationId")] Guid RegistrationId,
    [property: JsonPropertyName("email")]          string Email,
    [property: JsonPropertyName("error")]          string Error,
    [property: JsonPropertyName("failedAt")]       DateTimeOffset FailedAt
);