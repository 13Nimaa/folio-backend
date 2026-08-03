using System.Security.Cryptography;

namespace BooksProject.Authentication;

// PBKDF2 (RFC 2898) hashing. Stored format: {iterations}.{base64 salt}.{base64 hash}
public static class PasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 210_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA512;

    public static string Hash(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            Iterations,
            Algorithm,
            HashSize);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string password, string storedHash)
    {
        var parts = storedHash.Split('.', 3);

        if (parts.Length != 3 || !int.TryParse(parts[0], out int iterations))
        {
            return false;
        }

        byte[] salt;
        byte[] expectedHash;

        try
        {
            salt = Convert.FromBase64String(parts[1]);
            expectedHash = Convert.FromBase64String(parts[2]);
        }
        catch (FormatException)
        {
            return false;
        }

        byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password,
            salt,
            iterations,
            Algorithm,
            expectedHash.Length);

        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
