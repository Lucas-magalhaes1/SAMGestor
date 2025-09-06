using System.Globalization;
using System.Text;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Infrastructure.Services;

public sealed class HeuristicRelationshipService : IRelationshipService
{
    private readonly IRegistrationRepository _registrations;
    public HeuristicRelationshipService(IRegistrationRepository registrations)
        => _registrations = registrations;

    /// <summary>
    /// Ainda não implementado no MVP (depende de cadastro explícito).
    /// Mantemos false para não gerar falsos positivos.
    /// </summary>
    public Task<bool> AreSpousesAsync(Guid id1, Guid id2, CancellationToken ct = default)
        => Task.FromResult(false);

    /// <summary>
    /// Heurística "forte" de parentesco:
    /// - sobrenome igual (normalizado)
    /// E
    /// - (mesmo telefone) OU (mesma cidade)
    /// Retorna true somente quando a combinação é forte.
    /// </summary>
    public async Task<bool> AreDirectRelativesAsync(Guid id1, Guid id2, CancellationToken ct = default)
    {
        if (id1 == id2) return false;

        var r1 = await _registrations.GetByIdAsync(id1, ct);
        var r2 = await _registrations.GetByIdAsync(id2, ct);
        if (r1 is null || r2 is null) return false;

        var last1 = ExtractLastName((string)r1.Name);
        var last2 = ExtractLastName((string)r2.Name);

        var s1 = NormalizeSurname(last1);
        var s2 = NormalizeSurname(last2);

        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return false;
        if (!string.Equals(s1, s2, StringComparison.Ordinal)) return false;
        
        var samePhone = !string.IsNullOrWhiteSpace(r1.Phone)
                        && string.Equals(r1.Phone, r2.Phone, StringComparison.OrdinalIgnoreCase);

        var sameCity = !string.IsNullOrWhiteSpace(r1.City)
                       && string.Equals(Norm(r1.City), Norm(r2.City), StringComparison.Ordinal);

        return samePhone || sameCity;
    }
    

    private static string ExtractLastName(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;
        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? string.Empty : parts[^1];
    }

    private static string NormalizeSurname(string surname)
    {
        if (string.IsNullOrWhiteSpace(surname)) return string.Empty;
        var s = RemoveDiacritics(surname).Trim().ToLowerInvariant();

        // Ignora partículas isoladas comuns
        if (s is "de" or "da" or "do" or "dos" or "das") return string.Empty;

        return s;
    }

    private static string Norm(string s) => RemoveDiacritics(s).Trim().ToLowerInvariant();

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(capacity: normalized.Length);
        foreach (var c in normalized)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(c);
            if (uc != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }
}
