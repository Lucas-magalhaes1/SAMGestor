using SAMGestor.Domain.Commom;

public sealed class FullName : ValueObject
{
    public string Value { get; private set; } = null!;

    private string[]? _parts;

    private string[] PartsInternal
    {
        get
        {
            if (_parts is { Length: > 0 })
                return _parts;

            if (string.IsNullOrWhiteSpace(Value))
            {
                _parts = Array.Empty<string>();
                return _parts;
            }

            _parts = Value
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);

            return _parts;
        }
    }

    public string First  => PartsInternal[0];
    public string Last   => PartsInternal[^1];
    public IReadOnlyList<string> Parts => PartsInternal;
    
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