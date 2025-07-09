using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Domain.Specifications;

public sealed class WaitingListEligibilitySpecification : ISpecification<Registration>
{
    private readonly IWaitingListRepository _waiting;
    public WaitingListEligibilitySpecification(IWaitingListRepository waiting) => _waiting = waiting;

    public bool IsSatisfiedBy(Registration reg)
    {
        return reg.Status == RegistrationStatus.NotSelected &&
               !_waiting.Exists(reg.Id, reg.RetreatId);
    }
}