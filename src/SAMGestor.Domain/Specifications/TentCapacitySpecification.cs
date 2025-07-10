using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;

namespace SAMGestor.Domain.Specifications;

public sealed class TentCapacitySpecification : ISpecification<Tent>
{
    private readonly ITentRepository _tents;

    public TentCapacitySpecification(ITentRepository tents)
    {
        _tents = tents;
    }

    public bool IsSatisfiedBy(Tent tent)
    {
        var current = _tents.GetOccupancy(tent.Id);
        return current < tent.Capacity;
    }
}