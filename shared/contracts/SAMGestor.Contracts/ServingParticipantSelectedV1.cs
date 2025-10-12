using System.Text.Json.Serialization;

namespace SAMGestor.Contracts;

public sealed record ServingParticipantSelectedV1(
    [property: JsonPropertyName("registrationId")] Guid RegistrationId,
    [property: JsonPropertyName("retreatId")]      Guid RetreatId,
    [property: JsonPropertyName("amount")]         decimal Amount,
    [property: JsonPropertyName("currency")]       string Currency,
    [property: JsonPropertyName("name")]           string Name,
    [property: JsonPropertyName("email")]          string Email,
    [property: JsonPropertyName("phone")]          string? Phone
);