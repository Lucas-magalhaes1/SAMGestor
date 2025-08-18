using System.Text.Json.Serialization;

namespace SAMGestor.Contracts;

public sealed record SelectionParticipantSelectedV1(
    [property: JsonPropertyName("registrationId")] Guid RegistrationId,
    [property: JsonPropertyName("participantId")]  Guid ParticipantId,
    [property: JsonPropertyName("name")]           string Name,
    [property: JsonPropertyName("email")]          string Email,
    [property: JsonPropertyName("phone")]          string? Phone,
    [property: JsonPropertyName("retreatId")]      Guid RetreatId
);