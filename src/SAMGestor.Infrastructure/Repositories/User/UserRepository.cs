using Microsoft.EntityFrameworkCore;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories.User;

public sealed class UserRepository : IUserRepository
{
    private readonly SAMContext _db;

    public UserRepository(SAMContext db) => _db = db;

    public Task<Domain.Entities.User?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);

    public Task<Domain.Entities.User?> GetByEmailAsync(string email, CancellationToken ct = default)
        => _db.Users.FirstOrDefaultAsync(u => u.Email.Value == email, ct);

    public async Task AddAsync(Domain.Entities.User user, CancellationToken ct = default)
        => await _db.Users.AddAsync(user, ct);

    public Task UpdateAsync(Domain.Entities.User user, CancellationToken ct = default)
    {
        _db.Users.Update(user);
        return Task.CompletedTask;
    }
    
    public Task DeleteAsync(Domain.Entities.User user, CancellationToken ct = default)
    {
        _db.Users.Remove(user);
        return Task.CompletedTask;
    }
}