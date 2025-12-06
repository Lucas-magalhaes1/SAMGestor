using System.Text.RegularExpressions;

namespace SAMGestor.Application.Common.Validators;

public static class PasswordValidator
{
    private static readonly Regex UpperCase = new(@"[A-Z]", RegexOptions.Compiled);
    private static readonly Regex LowerCase = new(@"[a-z]", RegexOptions.Compiled);
    private static readonly Regex Digit = new(@"[0-9]", RegexOptions.Compiled);
    private static readonly Regex SpecialChar = new(@"[!@#$%^&*(),.?""':{}|<>_\-+=\[\]\\\/;`~]", RegexOptions.Compiled);

    public static (bool IsValid, string? ErrorMessage) Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return (false, "Senha é obrigatória");

        if (password.Length < 8)
            return (false, "A senha deve ter no mínimo 8 caracteres");

        if (password.Length > 128)
            return (false, "A senha deve ter no máximo 128 caracteres");

        if (!UpperCase.IsMatch(password))
            return (false, "A senha deve conter pelo menos uma letra maiúscula");

        if (!LowerCase.IsMatch(password))
            return (false, "A senha deve conter pelo menos uma letra minúscula");

        if (!Digit.IsMatch(password))
            return (false, "A senha deve conter pelo menos um número");

        if (!SpecialChar.IsMatch(password))
           return (false, "A senha deve conter pelo menos um caractere especial (!@#$%^&*...)");

        return (true, null);
    }

    /// <summary>
    /// Verifica se senha contém informações pessoais (nome, email)
    /// </summary>
    public static bool ContainsPersonalInfo(string password, string name, string email)
    {
        var lowerPassword = password.ToLowerInvariant();
        
        // Verifica se contém partes do nome (com 3+ caracteres)
        var nameParts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var part in nameParts)
        {
            if (part.Length >= 3 && lowerPassword.Contains(part.ToLowerInvariant()))
                return true;
        }

        // Verifica se contém parte do e-mail antes do @
        var emailUser = email.Split('@')[0];
        if (emailUser.Length >= 3 && lowerPassword.Contains(emailUser.ToLowerInvariant()))
            return true;

        return false;
    }
}
