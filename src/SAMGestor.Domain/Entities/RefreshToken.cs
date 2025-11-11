using SAMGestor.Domain.Commom;

namespace SAMGestor.Domain.Entities;

/// <summary>
/// Refresh token com rotação e revogação.
/// Armazenamos o HASH do token 
/// </summary>
public class RefreshToken : Entity<Guid>
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }         
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset? RevokedAt { get; private set; }
    public Guid? ReplacedByTokenId { get; private set; }
    
    public string? UserAgent { get; private set; }
    public string? IpAddress { get; private set; }
    
    public User User { get; private set; } = null!;

    private RefreshToken() { }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAt, DateTimeOffset createdAt, string? userAgent = null, string? ip = null)
        => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt,
            CreatedAt = createdAt,
            UserAgent = userAgent,
            IpAddress = ip
        };

    public bool IsActive(DateTimeOffset now) => RevokedAt == null && ExpiresAt > now;

    public void Revoke(DateTimeOffset now) => RevokedAt = now;

    public void ReplaceWith(Guid newTokenId, DateTimeOffset now)
    {
        Revoke(now);
        ReplacedByTokenId = newTokenId;
    }
}