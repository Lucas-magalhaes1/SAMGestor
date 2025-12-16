using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;

public sealed class EmailAddress : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public string Value { get; }

    private EmailAddress() { } // EF ok

    [JsonConstructor]
    public EmailAddress(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("E-mail não pode ser vazio", nameof(value));

        var normalized = value.Trim().ToLowerInvariant();

        if (!IsValid(normalized))
            throw new ArgumentException("E-mail inválido", nameof(value));

        Value = normalized;
    }

    public static bool IsValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalized = email.Trim();

        if (normalized.Length < 5 || normalized.Length > 254)
            return false;

        return EmailRegex.IsMatch(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(EmailAddress email) => email.Value;
    public override string ToString() => Value;
}