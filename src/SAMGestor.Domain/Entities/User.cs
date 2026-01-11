using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

/// <summary>
/// Usuário do sistema. 
/// confirmação de e-mail, lockout, contagem de falhas, navegações para tokens e foto de perfil.
/// </summary>

public class User : Entity<Guid>
{
    public FullName Name { get; private set; }
    public EmailAddress Email { get; private set; }
    public string Phone { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool Enabled { get; private set; }
    
    public bool EmailConfirmed { get; private set; }    
    public DateTimeOffset? EmailConfirmedAt { get; private set; }
    public int FailedAccessCount { get; private set; }           
    public DateTimeOffset? LockoutEndAt { get; private set; }    
    public DateTimeOffset? LastLoginAt { get; private set; }
    
    public string? PhotoStorageKey { get; private set; }
    public string? PhotoContentType { get; private set; }
    public int? PhotoSizeBytes { get; private set; }
    public DateTime? PhotoUploadedAt { get; private set; }
    
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
            FailedAccessCount = 0;
        }
    }

    public bool IsLocked(DateTimeOffset now) => LockoutEndAt.HasValue && LockoutEndAt.Value > now;

    public void ConfirmEmail(DateTimeOffset now)
    {
        EmailConfirmed = true;
        EmailConfirmedAt = now;
    }
    
    public void AddRefreshToken(RefreshToken token) => _refreshTokens.Add(token);
    public void AddEmailConfirmationToken(EmailConfirmationToken token) => _emailConfirmationTokens.Add(token);
    public void AddPasswordResetToken(PasswordResetToken token) => _passwordResetTokens.Add(token);
    
    public void ChangeEmail(EmailAddress newEmail)
    {
        Email = newEmail;
        EmailConfirmed = false;
        EmailConfirmedAt = null;
    }
    
    public void ChangeName(FullName name) => Name = name;

    public void ChangePhone(string phone) => Phone = phone.Trim();
    
    public void SetProfilePhoto(string storageKey, string contentType, int sizeBytes)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            throw new ArgumentException("Storage key não pode ser vazio", nameof(storageKey));
        
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type não pode ser vazio", nameof(contentType));
        
        if (sizeBytes <= 0)
            throw new ArgumentException("Tamanho do arquivo deve ser maior que zero", nameof(sizeBytes));

        PhotoStorageKey = storageKey;
        PhotoContentType = contentType;
        PhotoSizeBytes = sizeBytes;
        PhotoUploadedAt = DateTime.UtcNow;
    }

    public void RemoveProfilePhoto()
    {
        PhotoStorageKey = null;
        PhotoContentType = null;
        PhotoSizeBytes = null;
        PhotoUploadedAt = null;
    }

    public bool HasProfilePhoto() => !string.IsNullOrWhiteSpace(PhotoStorageKey);
}
