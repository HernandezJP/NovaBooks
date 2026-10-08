using System.Security.Cryptography;
using System.Text;

namespace NovaBooks.Infrastructure.Services.Authentication;

/// <summary>
/// El JWT no está cifrado; por eso se incluye un hash del
/// Security Stamp y no el valor original.
/// </summary>
public static class SecurityStampHasher
{
    public static string Hash(string? securityStamp)
    {
        byte[] hash = SHA256.HashData(
            Encoding.UTF8.GetBytes(securityStamp ?? string.Empty));

        return Convert.ToHexString(hash);
    }

    public static bool Matches(
        string? securityStamp,
        string? hashedStamp)
    {
        if (string.IsNullOrEmpty(securityStamp) ||
            string.IsNullOrEmpty(hashedStamp))
        {
            return false;
        }

        return CryptographicOperations.FixedTimeEquals(
            Encoding.ASCII.GetBytes(Hash(securityStamp)),
            Encoding.ASCII.GetBytes(hashedStamp));
    }
}
