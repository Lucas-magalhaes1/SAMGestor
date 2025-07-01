using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;

public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money() { }

    public Money(decimal amount, string currency = "BRL")
    {
        if (amount < 0) throw new ArgumentException(nameof(amount));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3) throw new ArgumentException(nameof(currency));
        Amount = decimal.Round(amount, 2);
        Currency = currency.ToUpperInvariant();
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }
}