using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;

namespace SAMGestor.Domain.Specifications;

/// <summary>
/// Depois que a inscrição é “confirmada” pelo gestor, ela
/// DEVE ficar com status PendingPayment. Qualquer outro
/// status nesse momento é inválido.
/// </summary>
public sealed class ConfirmedSetsPendingPaymentSpecification : ISpecification<Registration>
{
    public bool IsSatisfiedBy(Registration reg)
    {
        return reg.Status == RegistrationStatus.PendingPayment;
    }
}