using SAMGestor.Notification.Application.Abstractions;

namespace SAMGestor.Notification.Infrastructure.Payment;

public class FakePaymentLinkClient : IPaymentLinkClient
{
    public Task<string> CreatePaymentLinkAsync(Guid registrationId, CancellationToken ct)
    {
        var link = $"https://pay.local/r/{registrationId}";
        return Task.FromResult(link);
    }
}