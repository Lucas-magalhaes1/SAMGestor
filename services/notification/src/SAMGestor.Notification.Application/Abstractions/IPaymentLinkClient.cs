namespace SAMGestor.Notification.Application.Abstractions;

public interface IPaymentLinkClient
{
    Task<string> CreatePaymentLinkAsync(Guid registrationId, Guid participantId, CancellationToken ct);
}