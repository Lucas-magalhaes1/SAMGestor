using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task UpdateAsync(User user, CancellationToken ct = default);
    Task DeleteAsync(User user, CancellationToken ct = default);
    Task<(List<User> Users, int Total)> ListAsync(
        int skip, 
        int take, 
        string? search, 
        CancellationToken ct = default);
   
    
}