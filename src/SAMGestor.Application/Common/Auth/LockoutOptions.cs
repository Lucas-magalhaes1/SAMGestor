namespace SAMGestor.Application.Common.Auth;

/// <summary>
/// Configurações de lockout (bloqueio de conta por tentativas falhas).
/// </summary>
public sealed class LockoutOptions
{
    public const string SectionName = "Lockout";

    /// <summary>
    /// Número máximo de tentativas de login antes de bloquear.
    /// </summary>
    public int MaxFailedAttempts { get; set; } = 5;

    /// <summary>
    /// Duração do bloqueio em minutos.
    /// </summary>
    public int LockoutDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Tempo para resetar o contador de falhas (se não houver novas tentativas).
    /// </summary>
    public int ResetFailedAttemptsMinutes { get; set; } = 30;
}