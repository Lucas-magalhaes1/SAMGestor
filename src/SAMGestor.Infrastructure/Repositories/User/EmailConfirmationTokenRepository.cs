using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories.User;

public sealed class EmailConfirmationTokenRepository : IEmailConfirmationTokenRepository
{
    private readonly SAMContext _db;
    public EmailConfirmationTokenRepository(SAMContext db) => _db = db;

    public async Task AddAsync(EmailConfirmationToken token, CancellationToken ct = default)
        => await _db.EmailConfirmationTokens.AddAsync(token, ct);

    // buscando só pelo hash (como usamos no ConfirmEmailHandler)
    public Task<EmailConfirmationToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default)
        => _db.EmailConfirmationTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task UpdateAsync(EmailConfirmationToken token, CancellationToken ct = default)
    {
        _db.EmailConfirmationTokens.Update(token);
        return Task.CompletedTask;
    }
}