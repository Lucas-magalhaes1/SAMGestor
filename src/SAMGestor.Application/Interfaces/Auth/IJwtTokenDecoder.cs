namespace SAMGestor.Application.Interfaces.Auth;

public interface IJwtTokenDecoder
{
    /// <summary>
    /// Extrai o userId (claim "sub") de um JWT, mesmo se expirado.
    /// Lança exceção se o token for inválido (assinatura incorreta, mal formatado, etc).
    /// </summary>
    Guid ExtractUserIdFromExpiredToken(string accessToken);
}