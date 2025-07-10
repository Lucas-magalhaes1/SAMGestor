using System.Text.RegularExpressions;
using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Specifications;

/// <summary>
/// Valida se todos os placeholders do template pertencem
/// ao conjunto de placeholders suportados pelo sistema.
/// </summary>
public sealed class TemplatePlaceholdersValidSpecification
    : ISpecification<MessageTemplate>
{
    private static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "nome",
        "retiro",
        "metodoPagamento",
        "valor",
        "vencimento",
        "cidade"
    };

    private static readonly Regex PlaceholderRegex =
        new(@"\{\{\s*(\w+)\s*\}\}", RegexOptions.Compiled);

    public bool IsSatisfiedBy(MessageTemplate tpl)
    {
        
        if (!tpl.HasPlaceholders) return true;
        
        var matches = PlaceholderRegex.Matches(tpl.Content);
        
        if (matches.Count == 0) return false;
        
        return matches.All(m => Allowed.Contains(m.Groups[1].Value));
    }
}