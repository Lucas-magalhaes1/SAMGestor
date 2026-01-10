using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface IManualPaymentProofRepository
{
    Task AddAsync(ManualPaymentProof proof, CancellationToken ct = default);
    Task<ManualPaymentProof?> GetByRegistrationIdAsync(Guid registrationId, CancellationToken ct = default);
    Task<ManualPaymentProof?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<bool> ExistsByRegistrationIdAsync(Guid registrationId, CancellationToken ct = default);
}