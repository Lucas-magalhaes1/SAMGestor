using SAMGestor.Notification.Domain.Enums;

namespace SAMGestor.Notification.Domain.Entities;

public class SelectedRegistration
{
    public Guid RegistrationId { get; private set; }
    public Guid RetreatId      { get; private set; }

    public string  Name    { get; private set; } = default!;
    public string  Email   { get; private set; } = default!;
    public string? Phone   { get; private set; }
    public decimal Amount  { get; private set; }
    public string  Currency{ get; private set; } = "BRL";

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public SelectionKind Kind { get; private set; } = SelectionKind.Selection;

    private SelectedRegistration() { }

    public SelectedRegistration(Guid registrationId,
                                Guid retreatId,
                                string name,
                                string email,
                                string? phone,
                                decimal amount,
                                string currency,
                                SelectionKind kind = SelectionKind.Selection)
    {
        RegistrationId = registrationId;
        RetreatId      = retreatId;
        Kind           = kind;

        UpdateContact(name, email, phone);
        UpdatePricing(amount, currency);
        CreatedAt = UpdatedAt;
    }

    public void UpdateContact(string name, string email, string? phone)
    {
        Name  = name.Trim();
        Email = email.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Touch();
    }

    public void UpdatePricing(decimal amount, string currency)
    {
        Amount   = decimal.Round(amount, 2);
        Currency = string.IsNullOrWhiteSpace(currency) ? "BRL" : currency.ToUpperInvariant();
        Touch();
    }

    public void SetKind(SelectionKind kind)
    {
        Kind = kind;
        Touch();
    }

    public void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
