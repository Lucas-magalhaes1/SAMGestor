using MediatR;

namespace SAMGestor.Application.Features.Payments;

public sealed record RegisterManualPaymentCommand(
    Guid RegistrationId,
    string PaymentMethod,     
    DateTime PaymentDate,
    decimal Amount,
    string? Currency,          
    Stream FileStream,         // ← Stream puro, não IFormFile
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string? Notes
) : IRequest<RegisterManualPaymentResult>;