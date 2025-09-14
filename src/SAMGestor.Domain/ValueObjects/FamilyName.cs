using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;

public sealed class FamilyName : ValueObject
{
    public string Value { get; }
    private FamilyName() { }
    public FamilyName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(nameof(value));
        var v = value.Trim();
        if (v.Length > 120) throw new ArgumentException(nameof(value));
        Value = v;
    }
    protected override IEnumerable<object?> GetEqualityComponents()
    { yield return Value.ToLowerInvariant(); }
    public static implicit operator string(FamilyName n) => n.Value;
}