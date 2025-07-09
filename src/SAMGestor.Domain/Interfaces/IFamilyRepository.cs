using SAMGestor.Domain.Entities;

namespace SAMGestor.Domain.Interfaces;

public interface IFamilyRepository
{
    Family? GetById(Guid id);
    IReadOnlyCollection<Registration> GetMembers(Guid familyId);
}