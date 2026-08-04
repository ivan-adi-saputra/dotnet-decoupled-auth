using System.Security.Cryptography;

namespace AuthApp.Api.Services;

/// <summary>
/// Hashes passwords with PBKDF2 (built into .NET, no extra package required).
/// Stored format is "{iterations}.{saltBase64}.{hashBase64}" so each hash is
/// self-describing and future iteration-count increases don't break old hashes.
/// </summary>
public class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 600_000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hashedPassword)
    {
        var parts = hashedPassword.Split('.', 3);
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[1]);
            var expectedHash = Convert.FromBase64String(parts[2]);
            var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
