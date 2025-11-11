using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly SAMContext _db;
    public PasswordResetTokenRepository(SAMContext db) => _db = db;

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default)
        => await _db.PasswordResetTokens.AddAsync(token, ct);

    public Task<PasswordResetToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task UpdateAsync(PasswordResetToken token, CancellationToken ct = default)
    {
        _db.PasswordResetTokens.Update(token);
        return Task.CompletedTask;
    }
}