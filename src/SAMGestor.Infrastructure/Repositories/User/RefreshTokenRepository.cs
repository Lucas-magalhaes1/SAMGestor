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