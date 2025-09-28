using SAMGestor.Domain.Entities;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Interfaces;

public interface IServiceRegistrationRepository
{
    Task AddAsync(ServiceRegistration entity, CancellationToken ct = default);
    Task<bool> ExistsByCpfInRetreatAsync(CPF cpf, Guid retreatId, CancellationToken ct = default);
    Task<bool> ExistsByEmailInRetreatAsync(EmailAddress email, Guid retreatId, CancellationToken ct = default);
    Task<bool> IsCpfBlockedAsync(CPF cpf, CancellationToken ct = default);
    
    Task<IDictionary<Guid, int>> CountPreferencesBySpaceAsync(Guid retreatId, CancellationToken ct = default);
}