namespace SAMGestor.Notification.Application.Abstractions;

public interface ITemplateRenderer
{
    string Render(string template, IDictionary<string, string> variables);
}