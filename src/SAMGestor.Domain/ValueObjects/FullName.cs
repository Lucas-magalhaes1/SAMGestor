using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;

public sealed class FullName : ValueObject
{
    public string Value { get; }
    public string First  => _parts[0];
    public string Last   => _parts[^1];
    public IReadOnlyList<string> Parts => _parts;

    private readonly string[] _parts;

    private FullName() { }

    public FullName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 120)
            throw new ArgumentException(nameof(value));

        Value  = value.Trim();
        _parts = Value.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (_parts.Length < 2)                                 
            throw new ArgumentException(nameof(value));
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value.ToLowerInvariant();
    }

    public static implicit operator string(FullName name) => name.Value;
}