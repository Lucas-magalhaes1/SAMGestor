using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;
public sealed class TentNumber : ValueObject
{
    public int Value { get; }

    private TentNumber() { }

    public TentNumber(int value)
    {
        if (value <= 0) throw new ArgumentException(nameof(value));
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator int(TentNumber number) => number.Value;
}