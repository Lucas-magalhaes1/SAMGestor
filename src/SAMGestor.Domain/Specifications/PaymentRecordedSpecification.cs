using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Domain.Specifications;

/// <summary>
/// Valida o evento de pagamento recebido do gateway.
/// Só é considerado válido se o status final for PaymentConfirmed.
/// </summary>
public sealed class PaymentRecordedSpecification
    : ISpecification<Registration>
{
    public bool IsSatisfiedBy(Registration reg)
    {
        return reg.Status == RegistrationStatus.PaymentConfirmed;
    }
}