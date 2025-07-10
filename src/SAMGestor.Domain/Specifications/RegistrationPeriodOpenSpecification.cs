using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Domain.Specifications;

public sealed class RegistrationPeriodOpenSpecification : ISpecification<Registration>
{
    private readonly IRetreatRepository _retreatRepository;
    private readonly Func<DateTime> _clock;   

    public RegistrationPeriodOpenSpecification(
        IRetreatRepository retreatRepository,
        Func<DateTime>? clock = null)
    {
        _retreatRepository = retreatRepository;
        _clock = clock ?? (() => DateTime.UtcNow);   
    }

    public bool IsSatisfiedBy(Registration registration)
    {
        var retreat = _retreatRepository.GetById(registration.RetreatId);
        if (retreat is null)
            return false;   

        var today = DateOnly.FromDateTime(_clock());
        return retreat.RegistrationWindowOpen(today);
    }
}