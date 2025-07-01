
using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;

public sealed class Percentage : ValueObject
{
    public decimal Value { get; }

    private Percentage() { }

    public Percentage(decimal value)
    {
        if (value < 0 || value > 100) throw new ArgumentException(nameof(value));
        Value = decimal.Round(value, 2);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator decimal(Percentage pct) => pct.Value;
}