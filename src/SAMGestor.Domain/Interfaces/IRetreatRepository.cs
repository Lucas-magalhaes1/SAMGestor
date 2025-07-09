using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface IRetreatRepository
{
    Retreat? GetById(Guid id);
}