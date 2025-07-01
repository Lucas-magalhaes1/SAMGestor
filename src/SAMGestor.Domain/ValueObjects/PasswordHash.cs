using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;

public sealed class PasswordHash : ValueObject
{
    public string Value { get; }

    private PasswordHash() { }

    public PasswordHash(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length < 60) throw new ArgumentException(nameof(value));
        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(PasswordHash hash) => hash.Value;
}