using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;
public sealed class EmailAddress : ValueObject
{
    public string Value { get; }
    private EmailAddress() { Value = null!; }  
    public EmailAddress(string value)
    {
        try { _ = new System.Net.Mail.MailAddress(value); }
        catch { throw new ArgumentException(nameof(value)); }
        Value = value.ToLowerInvariant();
    }
    protected override IEnumerable<object?> GetEqualityComponents() { yield return Value; }
    public static implicit operator string(EmailAddress email) => email.Value;
}