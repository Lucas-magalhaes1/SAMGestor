using SAMGestor.Domain.Entities;
using SAMGestor.Domain.Interfaces;
using SAMGestor.Domain.ValueObjects;
using SAMGestor.Infrastructure.Persistence;

namespace SAMGestor.Infrastructure.Repositories
{
    public class RegistrationRepository : IRegistrationRepository
    {
        private readonly SAMContext _context;

        public RegistrationRepository(SAMContext context)
        {
            _context = context;
        }

        public Registration? GetById(Guid id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Registration> GetAllByRetreatId(Guid retreatId)
        {
            throw new NotImplementedException();
        }

        public Registration? GetByCpfAndRetreat(CPF cpf, Guid retreatId)
        {
            throw new NotImplementedException();
        }

        public bool IsCpfBlocked(CPF cpf)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Registration> GetAllByCpf(CPF cpf)
        {
            throw new NotImplementedException();
        }
    }
}