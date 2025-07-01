

using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.Entities;

public class ChangeLog : Entity<Guid>
{
    public string EntityName { get; private set; }
    public Guid EntityId { get; private set; }
    public Guid UserId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string Description { get; private set; }

    private ChangeLog() { }

    public ChangeLog(string entityName, Guid entityId, Guid userId, string description)
    {
        Id = Guid.NewGuid();
        EntityName = entityName.Trim();
        EntityId = entityId;
        UserId = userId;
        OccurredAt = DateTime.UtcNow;
        Description = description.Trim();
    }
}