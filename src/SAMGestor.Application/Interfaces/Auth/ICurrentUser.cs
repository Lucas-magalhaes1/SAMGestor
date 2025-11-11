namespace SAMGestor.Application.Interfaces.Auth;

/// <summary>
/// Acesso ao usuário atual (Claims) em Handlers.
/// </summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool? EmailConfirmed { get; }
}