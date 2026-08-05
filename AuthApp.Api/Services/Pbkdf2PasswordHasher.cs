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

    // Computed once per process (this class is registered as a singleton), not per
    // request — otherwise every login would pay this cost twice (once here, once for the
    // real Verify() call), doubling response time across the board instead of just
    // equalizing the two branches that matter.
    public string DummyHash { get; } = ComputeDummyHash();

    private static string ComputeDummyHash()
    {
        // The password hashed here is arbitrary and not a secret — nothing is ever
        // authenticated against it for real. It only exists to give Verify() a
        // realistic, correctly-formatted target to hash against.
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2("dummy-password-for-timing-safety", salt, Iterations, Algorithm, HashSize);
        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

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
