using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;
public sealed class CPF : ValueObject
{
    public string Value { get; }
    private CPF() { }
    public CPF(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length != 11) throw new ArgumentException(nameof(value));
        Value = digits;
    }
    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
    public static implicit operator string(CPF cpf) => cpf.Value;
}