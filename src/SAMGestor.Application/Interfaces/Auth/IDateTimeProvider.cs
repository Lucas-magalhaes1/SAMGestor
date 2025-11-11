namespace SAMGestor.Application.Interfaces.Auth;

/// <summary>
/// Facilita testes e padroniza horário (UTC).
/// </summary>
public interface IDateTimeProvider
{
    DateTimeOffset UtcNow { get; }
}