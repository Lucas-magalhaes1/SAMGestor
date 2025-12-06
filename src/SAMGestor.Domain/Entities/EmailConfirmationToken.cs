using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.Entities;

public class EmailConfirmationToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UsedAt { get; private set; }

    public User User { get; private set; } = null!;

    private EmailConfirmationToken() { }

    public static EmailConfirmationToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt
        };

    public bool IsValid(DateTimeOffset now) => UsedAt == null && ExpiresAt > now;

    public void MarkUsed(DateTimeOffset now) => UsedAt = now;
    
    public void ForceExpire()
    {
        ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1);
    }
    
}