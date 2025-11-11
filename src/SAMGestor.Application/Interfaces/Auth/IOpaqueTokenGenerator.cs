namespace SAMGestor.Application.Interfaces.Auth;

/// <summary>
/// Gera tokens opacos criptograficamente fortes (para refresh/reset/confirm).
/// </summary>
public interface IOpaqueTokenGenerator
{
    string GenerateSecureToken(int bytesLength = 32); // 256 bits por default
}