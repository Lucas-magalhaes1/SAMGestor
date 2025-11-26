namespace SAMGestor.Payment.Infrastructure.Options;

public class MercadoPagoOptions
{
    public string AccessToken { get; set; } = default!;
    public bool UseSandbox { get; set; } = true;
}