using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Domain.Specifications;

/// <summary>
/// Valida que uma inscrição recém-confirmada foi
/// corretamente marcada como PendingPayment.
/// Chame logo após o comando/handler “ConfirmParticipation”.
/// </summary>
public sealed class ConfirmedSetsPendingPaymentSpecification
    : ISpecification<Registration>
{
    public bool IsSatisfiedBy(Registration reg)
    {
        // Se a inscrição ainda NÃO foi confirmada, a regra não se aplica.
        if (reg.Status != RegistrationStatus.PendingPayment &&
            reg.Status != RegistrationStatus.Selected)
            return true;

        // Regra: depois do “Selected” deve ficar PendingPayment.
        return reg.Status == RegistrationStatus.PendingPayment;
    }
}