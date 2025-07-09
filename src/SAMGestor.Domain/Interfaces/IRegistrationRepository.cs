using SAMGestor.Domain.Entities;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Interfaces;

public interface IRegistrationRepository
{
    Registration? GetById(Guid id); 
    
    IEnumerable<Registration> GetAllByRetreatId(Guid retreatId);
    Registration? GetByCpfAndRetreat(CPF cpf, Guid retreatId);
    bool IsCpfBlocked(CPF cpf);
    
    IEnumerable<Registration> GetAllByCpf(CPF cpf);
}