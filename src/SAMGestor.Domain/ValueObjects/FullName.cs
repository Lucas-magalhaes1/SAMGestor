using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;
public sealed class FullName : ValueObject
{
    public string Value { get; }
    private FullName() { }
    public FullName(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 120) throw new ArgumentException(nameof(value));
        Value = value.Trim();
    }
    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value.ToLowerInvariant(); }
    public static implicit operator string(FullName name) => name.Value;
}