using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Domain.Entities;

public class MessageTemplate : Entity<Guid>
{
    public TemplateType Type { get; private set; }
    public string Content { get; private set; }
    public bool HasPlaceholders { get; private set; }

    private MessageTemplate() { }

    public MessageTemplate(TemplateType type, string content, bool hasPlaceholders)
    {
        Id = Guid.NewGuid();
        Type = type;
        Content = content.Trim();
        HasPlaceholders = hasPlaceholders;
    }
}