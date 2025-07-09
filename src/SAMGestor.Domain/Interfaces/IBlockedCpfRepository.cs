using SAMGestor.Domain.ValueObjects;

public interface IBlockedCpfRepository
{
    bool Exists(CPF cpf);
}