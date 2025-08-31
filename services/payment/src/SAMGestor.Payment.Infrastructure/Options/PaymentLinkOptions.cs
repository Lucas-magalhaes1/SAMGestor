namespace SAMGestor.Payment.Infrastructure.Options;

public sealed class PaymentLinkOptions
{
    // Base pública do link enviado ao participante
    public string PublicBaseUrl { get; set; } = "http://localhost:5002";
}