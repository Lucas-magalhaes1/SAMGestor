namespace SAMGestor.Application.Interfaces.Auth;

/// <summary>
/// Abstrai o hash/verify de senha. 
/// Se seu VO PasswordHash já resolve, este serviço pode apenas delegar.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string hash, string password);
}