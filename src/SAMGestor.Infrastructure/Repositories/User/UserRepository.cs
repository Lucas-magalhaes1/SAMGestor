using Microsoft.EntityFrameworkCore;
using SAMGestor.Application.Common.Pagination;
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
        return Task.CompletedTask;
    }

    
    public Task DeleteAsync(Domain.Entities.User user, CancellationToken ct = default)
    {
        _db.Users.Remove(user);
        return Task.CompletedTask;
    }
    
    
    public async Task<(List<Domain.Entities.User> Items, int TotalCount)> ListAsync(
        int skip, 
        int take, 
        string? search, 
        CancellationToken ct = default)
    {
        var query = _db.Users.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(u => 
                u.Name.Value.ToLower().Contains(searchLower) ||
                u.Email.Value.ToLower().Contains(searchLower));
        }

        var totalCount = await query.CountAsync(ct);
        
        var users = await query
            .OrderBy(u => u.Name.Value)
            .ApplyPagination(skip, take) 
            .ToListAsync(ct);

        return (users, totalCount);
    }
}