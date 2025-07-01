
using SAMGestor.Domain.Commom;
using SAMGestor.Domain.Enums;
using SAMGestor.Domain.ValueObjects;

namespace SAMGestor.Domain.Entities;

public class User : Entity<Guid>
{
    public FullName Name { get; private set; }
    public EmailAddress Email { get; private set; }
    public string Phone { get; private set; }
    public PasswordHash PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public bool Enabled { get; private set; }

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
    }

    public void Disable() => Enabled = false;
    public void Enable() => Enabled = true;
    public void ChangePassword(PasswordHash newHash) => PasswordHash = newHash;
}