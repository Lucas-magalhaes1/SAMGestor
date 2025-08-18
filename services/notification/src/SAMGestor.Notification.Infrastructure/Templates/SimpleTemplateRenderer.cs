using System.Text.RegularExpressions;
using SAMGestor.Notification.Application.Abstractions;

namespace SAMGestor.Notification.Infrastructure.Templates;

/// <summary>
/// Renderer simples: substitui {{Key}} por valores.
/// </summary>
public class SimpleTemplateRenderer : ITemplateRenderer
{
    private static readonly Regex Placeholder = new(@"\{\{([a-zA-Z0-9_]+)\}\}", RegexOptions.Compiled);

    public string Render(string template, IDictionary<string, string> variables)
    {
        return Placeholder.Replace(template, m =>
        {
            var key = m.Groups[1].Value;
            return variables.TryGetValue(key, out var val) ? val : m.Value;
        });
    }
}