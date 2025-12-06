using System.Text.RegularExpressions;
using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.ValueObjects;

public sealed class EmailAddress : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public string Value { get; }

    private EmailAddress() { } 

    public EmailAddress(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("E-mail não pode ser vazio", nameof(email));

        var normalized = email.Trim().ToLowerInvariant();
        
        if (!IsValid(normalized))
            throw new ArgumentException("E-mail inválido", nameof(email));

        Value = normalized;
    }

    /// <summary>
    /// Valida se o e-mail tem formato válido (sem criar instância)
    /// </summary>
    public static bool IsValid(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var normalized = email.Trim();
        
        // Validações básicas
        if (normalized.Length < 5 || normalized.Length > 254)
            return false;

        // Regex básico para formato de e-mail
        return EmailRegex.IsMatch(normalized);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public static implicit operator string(EmailAddress email) => email.Value;
    public override string ToString() => Value;
}