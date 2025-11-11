using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface IEmailConfirmationTokenRepository
{
    Task AddAsync(EmailConfirmationToken token, CancellationToken ct = default);
    Task<EmailConfirmationToken?> GetByHashAsync(string tokenHash, CancellationToken ct = default);
    Task UpdateAsync(EmailConfirmationToken token, CancellationToken ct = default);
}