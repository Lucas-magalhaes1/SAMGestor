using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories.User;

public sealed class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly SAMContext _db;
    public RefreshTokenRepository(SAMContext db) => _db = db;

    public async Task AddAsync(RefreshToken token, CancellationToken ct = default)
        => await _db.RefreshTokens.AddAsync(token, ct);

    public Task<RefreshToken?> GetByHashAsync(Guid userId, string tokenHash, CancellationToken ct = default)
        => _db.RefreshTokens.FirstOrDefaultAsync(t => t.UserId == userId && t.TokenHash == tokenHash, ct);
    
    public async Task<RefreshToken?> GetByHashWithLockAsync(Guid userId, string tokenHash, CancellationToken ct = default)
    {
        return await _db.RefreshTokens
            .FromSqlRaw(@"
                SELECT * FROM refresh_tokens 
                WHERE user_id = {0} AND token_hash = {1}
                FOR UPDATE
            ", userId, tokenHash)
            .FirstOrDefaultAsync(ct);
    }
    
    public Task<RefreshToken?> GetByIdAsync(Guid tokenId, CancellationToken ct = default)
        => _db.RefreshTokens.FirstOrDefaultAsync(t => t.Id == tokenId, ct);

    public Task UpdateAsync(RefreshToken token, CancellationToken ct = default)
    {
        _db.RefreshTokens.Update(token);
        return Task.CompletedTask;
    }

    public async Task<List<RefreshToken>> GetActiveTokensByUserIdAsync(Guid userId, DateTimeOffset now, CancellationToken ct = default)
        => await _db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null && t.ExpiresAt > now)
            .ToListAsync(ct);
}