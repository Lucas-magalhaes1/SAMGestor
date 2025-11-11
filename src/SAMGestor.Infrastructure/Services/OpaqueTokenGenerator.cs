using System.Security.Cryptography;
using SAMGestor.Application.Interfaces.Auth;

namespace SAMGestor.Infrastructure.Services;

public sealed class OpaqueTokenGenerator : IOpaqueTokenGenerator
{
    public string GenerateSecureToken(int bytesLength = 32)
    {
        var bytes = new byte[bytesLength];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string Base64UrlEncode(byte[] input)
        => Convert.ToBase64String(input)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
}