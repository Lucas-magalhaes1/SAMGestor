using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

/// <summary>
/// Usuário do sistema. 
/// Agora inclui: confirmação de e-mail, lockout, contagem de falhas e navegações para tokens.
/// </summary>
public class User : Entity<Guid>
{
    public FullName Name { get; private set; }
    public EmailAddress Email { get; private set; }
    public string Phone { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool Enabled { get; private set; }

    // Segurança / Sessão
    public bool EmailConfirmed { get; private set; }
    public DateTimeOffset? EmailConfirmedAt { get; private set; }
    public int FailedAccessCount { get; private set; }           // tentativas seguidas
    public DateTimeOffset? LockoutEndAt { get; private set; }    // se futuro => bloqueado até lá
    public DateTimeOffset? LastLoginAt { get; private set; }

    // Navegações para tokens
    private readonly List<RefreshToken> _refreshTokens = new();
    public IReadOnlyCollection<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private readonly List<EmailConfirmationToken> _emailConfirmationTokens = new();
    public IReadOnlyCollection<EmailConfirmationToken> EmailConfirmationTokens => _emailConfirmationTokens.AsReadOnly();

    private readonly List<PasswordResetToken> _passwordResetTokens = new();
    public IReadOnlyCollection<PasswordResetToken> PasswordResetTokens => _passwordResetTokens.AsReadOnly();

    private User() { }

    public User(FullName name, EmailAddress email, string phone, PasswordHash passwordHash, UserRole role)
    {
        Id = Guid.NewGuid();
        Name = name;
        Email = email;
        Phone = phone.Trim();
        PasswordHash = passwordHash;
        Role = role;
        Enabled = true;

        EmailConfirmed = false;
        FailedAccessCount = 0;
    }

    public void Disable() => Enabled = false;
    public void Enable() => Enabled = true;
    public void ChangePassword(PasswordHash newHash)
    {
        PasswordHash = newHash;
        // boa prática: resetar lock/contador ao trocar senha com sucesso
        FailedAccessCount = 0;
        LockoutEndAt = null;
    }

    public void SetRole(UserRole role) => Role = role;

    public void MarkLoginSuccess(DateTimeOffset now)
    {
        LastLoginAt = now;
        FailedAccessCount = 0;
        LockoutEndAt = null;
    }

    public void RegisterFailedLogin(DateTimeOffset now, int maxFailed = 5, TimeSpan? lockDuration = null)
    {
        FailedAccessCount++;
        if (FailedAccessCount >= maxFailed)
        {
            LockoutEndAt = now.Add(lockDuration ?? TimeSpan.FromMinutes(15));
            FailedAccessCount = 0; // zera o contador durante o lock
        }
    }

    public bool IsLocked(DateTimeOffset now) => LockoutEndAt.HasValue && LockoutEndAt.Value > now;

    public void ConfirmEmail(DateTimeOffset now)
    {
        EmailConfirmed = true;
        EmailConfirmedAt = now;
    }

    // Navegação: adicionar tokens (as fábricas dos tokens ficam nas entidades de token)
    public void AddRefreshToken(RefreshToken token) => _refreshTokens.Add(token);
    public void AddEmailConfirmationToken(EmailConfirmationToken token) => _emailConfirmationTokens.Add(token);
    public void AddPasswordResetToken(PasswordResetToken token) => _passwordResetTokens.Add(token);
}
