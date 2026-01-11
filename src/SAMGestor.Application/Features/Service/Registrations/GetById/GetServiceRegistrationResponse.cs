using SAMGestor.Application.Features.Registrations.GetById;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Application.Features.Service.Registrations.GetById;

public sealed record GetServiceRegistrationResponse(
    Guid   Id,
    Guid   RetreatId,
    string FullName,
    string Cpf,
    string Email,
    string Phone,
    DateOnly BirthDate,
    Gender Gender,
    string City,
    string Region,
    string? PhotoUrl,
    ServiceRegistrationStatus Status,
    bool   Enabled,
    DateTime RegistrationDateUtc,
    PreferredSpaceView? PreferredSpace,
    ManualPaymentProofDto? ManualPaymentProof 
);

public sealed record PreferredSpaceView(Guid Id, string Name );

public sealed record ManualPaymentProofDto(
    Guid ProofId,
    decimal Amount,
    string Currency,
    string Method,
    DateTime PaymentDate,
    DateTime UploadedAt,
    string? Notes,
    Guid RegisteredBy,
    DateTime RegisteredAt
);