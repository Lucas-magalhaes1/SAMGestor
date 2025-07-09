using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface ITentRepository
{
    Tent? GetById(Guid id);
    /// <summary>Número de participantes atualmente alocados na barraca.</summary>
    int GetOccupancy(Guid tentId);
}