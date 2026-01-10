using System.Text.Json.Serialization;

namespace SAMGestor.Contracts;

public sealed record ManualPaymentConfirmedV1(
    [property: JsonPropertyName("registrationId")] Guid RegistrationId,
    [property: JsonPropertyName("proofId")] Guid ProofId,
    [property: JsonPropertyName("retreatId")] Guid RetreatId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("email")] string Email,
    [property: JsonPropertyName("amount")] decimal Amount,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("paymentMethod")] string PaymentMethod,
    [property: JsonPropertyName("paymentDate")] DateTime PaymentDate,
    [property: JsonPropertyName("registeredBy")] string RegisteredBy
);