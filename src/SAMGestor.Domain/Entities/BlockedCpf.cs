using SAMGestor.Domain.Commom;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class BlockedCpf : Entity<Guid>
{
    public CPF Cpf { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private BlockedCpf() { }

    public BlockedCpf(CPF cpf)
    {
        Id = Guid.NewGuid();
        Cpf = cpf;
        CreatedAt = DateTime.UtcNow;
    }
}