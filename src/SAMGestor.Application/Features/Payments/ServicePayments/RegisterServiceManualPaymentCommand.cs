using MediatR;

namespace SAMGestor.Application.Features.Payments.ServicePayments;

public sealed record RegisterServiceManualPaymentCommand(
    Guid ServiceRegistrationId,
    string PaymentMethod,
    DateTime PaymentDate,
    decimal Amount,
    string? Currency,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string? Notes
) : IRequest<RegisterServiceManualPaymentResult>;