using System.Security.Cryptography;
using SAMGestor.Application.Interfaces.Auth;

namespace SAMGestor.Infrastructure.Services;

/// <summary>
/// PBKDF2 com salt aleatório. Formato: PBKDF2$iter$salt$hash
/// </summary>
public sealed class PasswordHasher : IPasswordHasher
{
    private const int DefaultIter = 120_000;
    private const int SaltBytes = 16;
    private const int KeyBytes  = 32;

    public string Hash(string password)
    {
        var salt = new byte[SaltBytes];
        RandomNumberGenerator.Fill(salt);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, DefaultIter, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(KeyBytes);

        return $"PBKDF2${DefaultIter}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public bool Verify(string hash, string password)
    {
        var parts = hash.Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2") return false;

        var iter = int.Parse(parts[1]);
        var salt = Convert.FromBase64String(parts[2]);
        var expected = Convert.FromBase64String(parts[3]);

        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iter, HashAlgorithmName.SHA256);
        var key = pbkdf2.GetBytes(expected.Length);

        return CryptographicOperations.FixedTimeEquals(key, expected);
    }
}