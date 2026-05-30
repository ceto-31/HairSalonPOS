using System.Security.Cryptography;
using System.Text;

namespace HairSalonPOS.Helpers;

public static class PasswordHasher
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int KeySize = 32;

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = DeriveKey(password, salt);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPassword(string password, string storedHash)
    {
        if (string.IsNullOrWhiteSpace(storedHash) || storedHash.Contains("placeholder", StringComparison.OrdinalIgnoreCase))
            return false;

        var parts = storedHash.Split('.');
        if (parts.Length != 3) return false;

        if (!int.TryParse(parts[0], out var iterations)) return false;

        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = DeriveKey(password, salt, iterations);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    private static byte[] DeriveKey(string password, byte[] salt, int iterations = Iterations)
    {
        return Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, KeySize);
    }
}
