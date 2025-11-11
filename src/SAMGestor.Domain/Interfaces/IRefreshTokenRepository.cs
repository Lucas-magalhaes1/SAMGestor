using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken ct = default);
    Task<RefreshToken?> GetByHashAsync(Guid userId, string tokenHash, CancellationToken ct = default);
    Task UpdateAsync(RefreshToken token, CancellationToken ct = default);
}